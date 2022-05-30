$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

function EnsureTrang {

   if (-not (Test-Path $trangPath -PathType Container)) {
      $trangTemp = Join-Path (Resolve-Path .) trang.zip
      [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
      Invoke-WebRequest https://github.com/relaxng/jing-trang/releases/download/V20181222/trang-20181222.zip -OutFile $trangTemp
      Expand-Archive $trangTemp $trangPath
      rm $trangTemp
   }
}

try {

   Add-Type -AssemblyName System.Xml.Linq

   $trangPath = Join-Path (Resolve-Path .) trang-

   EnsureTrang

   $trangJar = Resolve-Path $trangPath\*\trang.jar
   $xsdName = "xcst-app.xsd"

   java -jar $trangJar -o any-process-contents=lax -o indent=3 xcst-app.rng $xsdName

   $xsd = [Xml.Linq.XDocument]::Load((Resolve-Path $xsdName), 'PreserveWhitespace')
   [Xml.Linq.XComment]$comment = $xsd.Nodes() | where { $_ -is [Xml.Linq.XComment] } | select -First 1
   $comment.Value = " Converted from Relax NG schema, using Trang. Use only with code completion tools that do not support Relax NG. "
   $xsd.Save((Resolve-Path $xsdName))

} finally {
   Pop-Location
}
