Rem install choco

@powershell -NoProfile -ExecutionPolicy Bypass -Command "iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))" && SET PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin

rem install Nodejs with npm
choco install nodejs.install 

rem install markdown-pdf module
start /I npm install markdown-pdf
