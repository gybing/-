%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\installutil.exe FileReceiveService.exe
Net Start Service1
sc config Service1 start= auto