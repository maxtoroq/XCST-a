﻿$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$nuget = "..\.nuget\nuget.exe"
$solutionPath = Resolve-Path ..

try {

   ./ensure-nuget.ps1

   &$nuget restore $solutionPath\XCST-a.sln
   &$nuget restore ..\samples\aspnet\packages.config -SolutionDirectory ..

} finally {
   Pop-Location
}