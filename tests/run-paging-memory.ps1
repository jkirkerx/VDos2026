param([string]$SourcePath, [string]$MSBuildPath)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (!$SourcePath) { $SourcePath = Join-Path $repoRoot 'VDosApp/src/hardware/memory.cpp' }
if (!$MSBuildPath) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    $MSBuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (!$MSBuildPath) { throw 'Visual Studio MSBuild was not found.' }
$outputRoot = Join-Path $repoRoot 'Debug/paging-tests'
New-Item -ItemType Directory -Force $outputRoot | Out-Null
$source = Get-Content -LiteralPath $SourcePath -Raw
$start = $source.IndexOf('Bit8u Mem_Lodsb(LinPt addr)')
$end = $source.IndexOf('void Mem_Movsb(LinPt dest, LinPt src)')
if ($start -lt 0 -or $end -le $start) { throw 'Memory function boundaries not found.' }
$source.Substring($start, $end-$start) | Set-Content (Join-Path $outputRoot 'memory-access.inc')
# Keep diagnostic tests on current source when checking the pre-fix access functions.
$diagnostics = Get-Content (Join-Path $repoRoot 'VDosApp/src/hardware/memory.cpp') -Raw
$start = $diagnostics.IndexOf('static bool DiagnosticReadPhysicalDword')
$end = $diagnostics.IndexOf('static bool WritePageFaultDiagnostics')
$diagnostics.Substring($start, $end-$start) | Set-Content (Join-Path $outputRoot 'memory-diagnostics.inc')
Copy-Item (Join-Path $PSScriptRoot 'paging-memory.cpp') $outputRoot
@'
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations"><ProjectConfiguration Include="Debug|Win32"><Configuration>Debug</Configuration><Platform>Win32</Platform></ProjectConfiguration></ItemGroup>
  <PropertyGroup Label="Globals"><WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion></PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Label="Configuration"><ConfigurationType>Application</ConfigurationType><PlatformToolset>v145</PlatformToolset></PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <PropertyGroup><OutDir>$(ProjectDir)</OutDir><IntDir>$(ProjectDir)obj\</IntDir><TargetName>paging-memory</TargetName></PropertyGroup>
  <ItemDefinitionGroup><ClCompile><ExceptionHandling>Sync</ExceptionHandling><PreprocessorDefinitions>_CRT_SECURE_NO_WARNINGS</PreprocessorDefinitions></ClCompile><Link><SubSystem>Console</SubSystem></Link></ItemDefinitionGroup>
  <ItemGroup><ClCompile Include="paging-memory.cpp" /></ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
</Project>
'@ | Set-Content (Join-Path $outputRoot 'paging-memory.vcxproj')
& $MSBuildPath (Join-Path $outputRoot 'paging-memory.vcxproj') /nologo /v:minimal /p:Configuration=Debug /p:Platform=Win32
if ($LASTEXITCODE -ne 0) { throw 'Paging test build failed.' }
& (Join-Path $outputRoot 'paging-memory.exe')
exit $LASTEXITCODE
