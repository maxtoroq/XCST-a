$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

function script:EnsureTrang {

   if (-not (Test-Path $trangPath -PathType Container)) {
      Add-Type -AssemblyName System.IO.Compression.FileSystem
      $trangTemp = Join-Path (Resolve-Path .) trang.zip
      Invoke-WebRequest https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/jing-trang/trang-20091111.zip -OutFile $trangTemp
      [IO.Compression.ZipFile]::ExtractToDirectory($trangTemp, (Resolve-Path .))
      rm $trangTemp
   }
}

try {

   $saxonPath = (Resolve-Path ..\packages\Saxon-HE.*)[0]
   $trangPath = Join-Path (Resolve-Path .) trang-20091111

   EnsureTrang

   java -jar $trangPath\trang.jar -o any-process-contents=lax -o indent=3 xcst-app.rng xcst-app.xsd

   &"$saxonPath\tools\Transform" -s:xcst-app.xsd -xsl:xsd-comment.xsl -o:xcst-app.xsd

} finally {
   Pop-Location
}