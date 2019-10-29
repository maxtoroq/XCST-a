$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$nuget = "..\.nuget\nuget.exe"
$solutionPath = Resolve-Path ..

try {

   .\ensure-nuget.ps1

   &$nuget restore $solutionPath\XCST-a.sln

   foreach ($web in ls ..\samples\* -Directory) {

      $packagesPath = "$web\packages.config"

      if (-not (Test-Path $packagesPath)) {
         continue
      }

      &$nuget restore $packagesPath -SolutionDirectory ..

      [xml]$packagesDoc = Get-Content $packagesPath
      [Xml.XmlElement]$compilerPlatform = $packagesDoc.packages.package |
         where { $_.id -eq "Microsoft.CodeDom.Providers.DotNetCompilerPlatform" }

      if ($compilerPlatform -eq $null) {
         continue
      }

      $roslyn = "$web\Bin\roslyn\"

      if (-not (Test-Path $roslyn)) {
         md $roslyn | Out-Null
      }

      Copy-Item ..\packages\Microsoft.Net.Compilers.*\tools\*.* $roslyn
      Write-Host Restored $roslyn
   }

} finally {
   Pop-Location
}
