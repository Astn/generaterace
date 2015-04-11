# generaterace 

usage:

GenerateRace.exe 50000 10 > race50kx10.json

Will generate reads for 50,000 entrants over 10 timing points. Timing points are set to 1 mile distance.

sample output:

{"bib":10000,"checkpoint":0,"time":"00:00:00"}
{"bib":7144,"checkpoint":1,"time":"00:06:07.5040000"}

** Streaming**

input: stdout from GenerateRace

sample output:
{"groupName":"female Checkpoint 1","bib":1238,"time":"00:10:54.8260000","age":16}
{"groupName":"female 13-18 Checkpoint 1","bib":1238,"time":"00:10:54.8260000","age":16}
{"groupName":"Overall Checkpoint 1","bib":4392,"time":"00:10:54.9010000","age":53}

** StreamingFSharp

usage:

.\GenerateRace\bin\Release\GenerateRace.exe 5000 5 | .\StreamingFSharp\bin\Release\StreamingFSharp.exe

sample output:
{"groupName":"female Checkpoint 1","bib":1238,"time":"00:10:54.8260000","age":16}
{"groupName":"female 13-18 Checkpoint 1","bib":1238,"time":"00:10:54.8260000","age":16}
{"groupName":"Overall Checkpoint 1","bib":4392,"time":"00:10:54.9010000","age":53}

** StreamingCSharp

usage:

.\GenerateRace\bin\Release\GenerateRace.exe 5000 5 | .\StreamingCSharp\bin\Release\StreamingCSharp.exe
