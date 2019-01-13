$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$nuget = "..\.nuget\nuget.exe"
$solutionPath = Resolve-Path ..

try {

   .\ensure-nuget.ps1

   &$nuget restore $solutionPath\XCST-a.sln

   foreach ($web in ls ..\samples\* -Directory) {

      &$nuget restore $web\packages.config -SolutionDirectory ..

      $roslyn = "$web\Bin\roslyn\"

      if (-not (Test-Path $roslyn)) {
         md $roslyn | Out-Null
      }

      Copy-Item ..\packages\Microsoft.Net.Compilers.*\tools\*.* $roslyn
   }

} finally {
   Pop-Location
}
