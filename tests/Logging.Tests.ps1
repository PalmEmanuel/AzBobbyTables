BeforeAll {
    # Load the module and reach into AzBobbyTables.Core via reflection.
    #
    # The Core assembly is loaded into the module's private AssemblyLoadContext and its types
    # are deliberately not resolvable from the session, so the pattern from PageSize.Tests.ps1
    # is reused here: pull the assembly out of the ALC, then GetType by name.
    Get-AzDataTableSupportedEntityType | Out-Null   # forces Core to load

    $CoreAssembly = foreach ($Context in [System.Runtime.Loader.AssemblyLoadContext]::All) {
        foreach ($Assembly in $Context.Assemblies) {
            if ($Assembly.GetName().Name -eq 'AzBobbyTables.Core') { $Assembly }
        }
    }
    $Script:CoreAssembly = @($CoreAssembly)[0]

    $Script:LogLevelType = $Script:CoreAssembly.GetType('PipeHow.AzBobbyTables.Core.Logging.PSLogLevel')
    $Script:LogEventType = $Script:CoreAssembly.GetType('PipeHow.AzBobbyTables.Core.Logging.PSLogEvent')
    $Script:LogSinkType  = $Script:CoreAssembly.GetType('PipeHow.AzBobbyTables.Core.Logging.IPSLogSink')
    $Script:ServiceType  = $Script:CoreAssembly.GetType('PipeHow.AzBobbyTables.Core.AzDataTableService')
}

Describe 'Core logging contract' {
    Context 'PSLogLevel enum' {
        It 'is defined and exposes the documented levels' {
            $Script:LogLevelType | Should -Not -BeNullOrEmpty
            $names = [System.Enum]::GetNames($Script:LogLevelType)
            foreach ($expected in 'Verbose', 'Warning', 'Debug', 'Information', 'Error') {
                $names | Should -Contain $expected
            }
        }
    }

    Context 'PSLogEvent' {
        It 'exposes Level, Message, Code and Context and preserves them' {
            $Script:LogEventType | Should -Not -BeNullOrEmpty

            $verbose = [System.Enum]::Parse($Script:LogLevelType, 'Verbose')
            $evt = [System.Activator]::CreateInstance(
                $Script:LogEventType,
                @($verbose, 'hello', 'CODE1', 'ctx-value'))

            $evt.Level.ToString()  | Should -Be 'Verbose'
            $evt.Message           | Should -Be 'hello'
            $evt.Code              | Should -Be 'CODE1'
            $evt.Context           | Should -Be 'ctx-value'
        }

        It 'rejects a null message' {
            $verbose = [System.Enum]::Parse($Script:LogLevelType, 'Verbose')
            {
                [System.Activator]::CreateInstance(
                    $Script:LogEventType,
                    @($verbose, $null, $null, $null))
            } | Should -Throw
        }
    }

    Context 'IPSLogSink' {
        It 'is defined with a single Log method' {
            $Script:LogSinkType | Should -Not -BeNullOrEmpty
            $method = $Script:LogSinkType.GetMethod('Log')
            $method | Should -Not -BeNullOrEmpty
            $method.GetParameters().Length | Should -Be 1
            $method.GetParameters()[0].ParameterType.FullName | Should -Be 'PipeHow.AzBobbyTables.Core.Logging.PSLogEvent'
        }
    }
}

Describe 'PSCmdletLogSink stream mapping' {
    BeforeAll {
        # The adapter lives in the AzBobbyTables.PS assembly (default ALC), not in Core.
        $PSAssembly = foreach ($Context in [System.Runtime.Loader.AssemblyLoadContext]::All) {
            foreach ($Assembly in $Context.Assemblies) {
                if ($Assembly.GetName().Name -eq 'AzBobbyTables.PS') { $Assembly }
            }
        }
        $Script:PSAssembly = @($PSAssembly)[0]
        $Script:AdapterType = $Script:PSAssembly.GetType('PipeHow.AzBobbyTables.Logging.PSCmdletLogSink')
        $Script:AdapterType | Should -Not -BeNullOrEmpty
    }

    It 'forwards each level to the matching PowerShell stream via ICommandRuntime' {
        # The adapter calls Cmdlet.WriteVerbose/WriteWarning/... which delegate to the cmdlet's
        # ICommandRuntime. Substitute a recording runtime to observe what the adapter emits
        # without needing a live pipeline. Only the members the adapter uses are implemented;
        # everything else throws NotImplementedException, which flags accidental fan-out.
        $runtimeSource = @"
using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Host;

public sealed class RecordingRuntime : ICommandRuntime2
{
    public ArrayList Verboses = new ArrayList();
    public ArrayList Warnings = new ArrayList();
    public ArrayList Debugs = new ArrayList();
    public ArrayList Informations = new ArrayList();
    public ArrayList Errors = new ArrayList();

    public PSHost Host { get { return null; } }
    public PSTransactionContext CurrentPSTransaction { get { return null; } }

    public void WriteVerbose(string text)              { Verboses.Add(text); }
    public void WriteWarning(string text)              { Warnings.Add(text); }
    public void WriteDebug(string text)                { Debugs.Add(text); }
    public void WriteInformation(InformationRecord r)  { Informations.Add(r); }
    public void WriteError(ErrorRecord r)              { Errors.Add(r); }

    public void WriteObject(object sendToPipeline)                                 { throw new NotImplementedException(); }
    public void WriteObject(object sendToPipeline, bool enumerateCollection)       { throw new NotImplementedException(); }
    public void WriteProgress(ProgressRecord r)                                    { throw new NotImplementedException(); }
    public void WriteProgress(long sourceId, ProgressRecord r)                     { throw new NotImplementedException(); }
    public void WriteCommandDetail(string text)                                    { throw new NotImplementedException(); }
    public bool ShouldProcess(string target)                                       { return true; }
    public bool ShouldProcess(string target, string action)                        { return true; }
    public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption)                                { return true; }
    public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason reason){ reason = ShouldProcessReason.None; return true; }
    public bool ShouldContinue(string query, string caption)                                                                    { return true; }
    public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll)                               { return true; }
    public bool ShouldContinue(string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll)       { return true; }
    public bool TransactionAvailable()                                             { return false; }
    public void ThrowTerminatingError(ErrorRecord r)                               { throw new NotImplementedException(); }
}

public sealed class HostCmdlet : Cmdlet {}
"@
        $refs = @(
            [System.Reflection.Assembly]::Load('netstandard').Location
            [object].Assembly.Location
            [System.Collections.ArrayList].Assembly.Location
            [System.Management.Automation.Cmdlet].Assembly.Location
        )
        Add-Type -TypeDefinition $runtimeSource -ReferencedAssemblies $refs -Language CSharp -PassThru | Out-Null

        $cmdlet  = New-Object 'HostCmdlet'
        $runtime = New-Object 'RecordingRuntime'
        $cmdlet.CommandRuntime = $runtime

        $adapterCtor = $Script:AdapterType.GetConstructor(@([System.Management.Automation.Cmdlet]))
        $adapterCtor | Should -Not -BeNullOrEmpty
        $adapter = $adapterCtor.Invoke(@([System.Management.Automation.Cmdlet]$cmdlet))

        # Null event must be a no-op.
        { $adapter.Log($null) } | Should -Not -Throw

        $verbose     = [System.Enum]::Parse($Script:LogLevelType, 'Verbose')
        $warning     = [System.Enum]::Parse($Script:LogLevelType, 'Warning')
        $debug       = [System.Enum]::Parse($Script:LogLevelType, 'Debug')
        $information = [System.Enum]::Parse($Script:LogLevelType, 'Information')
        $errorLevel  = [System.Enum]::Parse($Script:LogLevelType, 'Error')

        $adapter.Log([System.Activator]::CreateInstance($Script:LogEventType, @($verbose,     'v-msg', $null,      $null)))
        $adapter.Log([System.Activator]::CreateInstance($Script:LogEventType, @($warning,     'w-msg', $null,      $null)))
        $adapter.Log([System.Activator]::CreateInstance($Script:LogEventType, @($debug,       'd-msg', $null,      $null)))
        $adapter.Log([System.Activator]::CreateInstance($Script:LogEventType, @($information, 'i-msg', 'INFO-TAG', $null)))
        $adapter.Log([System.Activator]::CreateInstance($Script:LogEventType, @($errorLevel,  'e-msg', 'ERR-ID',   'ctx')))

        $runtime.Verboses     | Should -Contain 'v-msg'
        $runtime.Warnings     | Should -Contain 'w-msg'
        $runtime.Debugs       | Should -Contain 'd-msg'
        $runtime.Informations.Count | Should -Be 1
        $runtime.Informations[0].MessageData | Should -Be 'i-msg'
        $runtime.Informations[0].Tags        | Should -Contain 'INFO-TAG'

        $runtime.Errors.Count | Should -Be 1
        $runtime.Errors[0].FullyQualifiedErrorId | Should -Match 'ERR-ID'
        $runtime.Errors[0].Exception.Message     | Should -Be 'e-msg'
        $runtime.Errors[0].TargetObject          | Should -Be 'ctx'
    }
}

Describe 'AzDataTableService log emission' {
    BeforeAll {
        # Instantiate the service via its private constructor (CancellationToken) so we don't
        # need a live TableClient. The log helpers do not touch the client, only the sink.
        $ctor = $Script:ServiceType.GetConstructor(
            [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::NonPublic,
            $null,
            @([System.Threading.CancellationToken]),
            $null)
        $ctor | Should -Not -BeNullOrEmpty
        $Script:Service = $ctor.Invoke(@([System.Threading.CancellationToken]::None))

        $Script:LogSinkProperty = $Script:ServiceType.GetProperty('LogSink')

        $flags = [System.Reflection.BindingFlags]::Instance `
            -bor [System.Reflection.BindingFlags]::NonPublic `
            -bor [System.Reflection.BindingFlags]::Public
        $Script:LogVerboseMethod     = $Script:ServiceType.GetMethod('LogVerbose', $flags)
        $Script:LogWarningMethod     = $Script:ServiceType.GetMethod('LogWarning', $flags)
        $Script:LogDebugMethod       = $Script:ServiceType.GetMethod('LogDebug', $flags)
        $Script:LogInformationMethod = $Script:ServiceType.GetMethod('LogInformation', $flags)
        $Script:LogErrorMethod       = $Script:ServiceType.GetMethod('LogError', $flags)
    }

    It 'exposes a public settable LogSink property' {
        $Script:LogSinkProperty | Should -Not -BeNullOrEmpty
        $Script:LogSinkProperty.CanRead  | Should -BeTrue
        $Script:LogSinkProperty.CanWrite | Should -BeTrue
        $Script:LogSinkProperty.PropertyType.FullName | Should -Be 'PipeHow.AzBobbyTables.Core.Logging.IPSLogSink'
    }

    It 'rejects a null sink assignment' {
        # Core is only consumed by the PowerShell module, which always attaches a sink; assigning
        # null must fail fast rather than silently disable logging. TargetInvocationException wraps
        # the ArgumentNullException raised by the setter.
        {
            $Script:LogSinkProperty.SetValue($Script:Service, $null)
        } | Should -Throw
    }

    It 'forwards each helper call to the sink with the matching level' {
        # Build a dynamic proxy for IPSLogSink that captures each event in a list.
        # Compiling one-off IL is heavier than we need; a small assembly generated from source is
        # enough because IPSLogSink is public and Core is already loaded.
        $sinkSource = @"
using PipeHow.AzBobbyTables.Core.Logging;
public sealed class CapturingSink : IPSLogSink
{
    public System.Collections.ArrayList Events = new System.Collections.ArrayList();
    public void Log(PSLogEvent logEvent) { Events.Add(logEvent); }
}
"@
        $refs = @(
            [System.Reflection.Assembly]::Load('netstandard').Location
            [object].Assembly.Location
            [System.Collections.ArrayList].Assembly.Location
            $Script:CoreAssembly.Location
        )
        Add-Type -TypeDefinition $sinkSource -ReferencedAssemblies $refs -Language CSharp -PassThru | Out-Null
        $sink = New-Object 'CapturingSink'

        $Script:LogSinkProperty.SetValue($Script:Service, $sink)

        $Script:LogVerboseMethod.Invoke($Script:Service,     @('v-msg', 'V-CODE', 'v-ctx'))
        $Script:LogWarningMethod.Invoke($Script:Service,     @('w-msg', $null,    $null))
        $Script:LogDebugMethod.Invoke($Script:Service,       @('d-msg', $null,    $null))
        $Script:LogInformationMethod.Invoke($Script:Service, @('i-msg', $null,    $null))
        $Script:LogErrorMethod.Invoke($Script:Service,       @('e-msg', 'E-CODE', 'e-ctx'))

        $sink.Events.Count | Should -Be 5

        $sink.Events[0].Level.ToString() | Should -Be 'Verbose'
        $sink.Events[0].Message          | Should -Be 'v-msg'
        $sink.Events[0].Code             | Should -Be 'V-CODE'
        $sink.Events[0].Context          | Should -Be 'v-ctx'

        $sink.Events[1].Level.ToString() | Should -Be 'Warning'
        $sink.Events[2].Level.ToString() | Should -Be 'Debug'
        $sink.Events[3].Level.ToString() | Should -Be 'Information'

        $sink.Events[4].Level.ToString() | Should -Be 'Error'
        $sink.Events[4].Code             | Should -Be 'E-CODE'
    }
}
