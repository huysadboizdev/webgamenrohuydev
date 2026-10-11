$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$source = Join-Path $taskRoot 'Assets'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot '.build'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sources = @(
    (Join-Path $PSScriptRoot 'WrapperRunner.cs'),
    (Join-Path $PSScriptRoot 'WrapperStubs.cs'),
    (Join-Path $source 'Editor\WebProtocolCases.cs'),
    (Join-Path $source 'Editor\WebSessionCases.cs'),
    (Join-Path $source 'Editor\WebTransportRegression.cs'),
    (Join-Path $source 'Scripts\Assembly-CSharp\Web\ProtocolCodec.cs'),
    (Join-Path $source 'Scripts\Assembly-CSharp\Web\WebSessionCore.cs')
)
foreach ($game in @('Game1', 'Game2')) {
    foreach ($session in @('Session_ME', 'Session_ME2')) {
        $sources += Join-Path $source "Scripts\Assembly-CSharp\$game\$session.cs"
    }
}
$runner = Join-Path $output 'transport-tests.exe'
& $compiler '/nologo' '/define:NRO_WEB_TRANSPORT_TEST' "/out:$runner" @sources
if ($LASTEXITCODE -ne 0) { throw 'C# transport compilation failed' }
& $runner
if ($LASTEXITCODE -ne 0) { throw 'C# transport regression failed' }
& node (Join-Path $PSScriptRoot 'websocket-tests.cjs')
if ($LASTEXITCODE -ne 0) { throw 'Browser websocket regression failed' }
& node (Join-Path $PSScriptRoot 'wrapper-contract-tests.cjs')
if ($LASTEXITCODE -ne 0) { throw 'Wrapper contract regression failed' }
