$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$nuget = "..\.nuget\nuget.exe"
$solutionPath = Resolve-Path ..

try {

   ./ensure-nuget.ps1

   &$nuget restore $solutionPath\XCST-a.sln

   foreach ($web in ls ..\samples\* -Directory) {
      &$nuget restore $web\packages.config -SolutionDirectory ..
      Copy-Item ..\packages\Microsoft.Net.Compilers.*\tools\*.dll $web\Bin\roslyn
      Copy-Item ..\packages\Microsoft.Net.Compilers.*\tools\*.exe $web\Bin\roslyn
   }

} finally {
   Pop-Location
}
