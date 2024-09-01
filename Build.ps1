dotnet build

Remove-Item "..\ModBuild\Kapitalism" -Recurse

New-Item "..\ModBuild\Kapitalism" -ItemType "directory"
New-Item "..\ModBuild\Kapitalism\BepInEx" -ItemType "directory"

New-Item "..\ModBuild\Kapitalism\BepInEx\patchers" -ItemType "directory"
New-Item "..\ModBuild\Kapitalism\BepInEx\patchers\KapitalismPatcher" -ItemType "directory"

New-Item "..\ModBuild\Kapitalism\BepInEx\plugins" -ItemType "directory"
New-Item "..\ModBuild\Kapitalism\BepInEx\plugins\Kapitalism" -ItemType "directory"



Copy-Item "..\KapitalsimPatcher\bin\Debug\netstandard2.1\KaptialismPatcher.dll" "..\ModBuild\Kapitalism\BepInEx\patchers\KapitalismPatcher"
Copy-Item "..\KapitalsimPatcher\bin\Debug\netstandard2.1\KaptialismPatcher.pdb" "..\ModBuild\Kapitalism\BepInEx\patchers\KapitalismPatcher"

Copy-Item ".\bin\Debug\netstandard2.1\Kapitalism.dll" "..\ModBuild\Kapitalism\BepInEx\plugins\Kapitalism"
Copy-Item ".\bin\Debug\netstandard2.1\Kapitalism.pdb" "..\ModBuild\Kapitalism\BepInEx\plugins\Kapitalism"

Copy-Item "..\ModBuild\ModData\Kapitalism\*" "..\ModBuild\Kapitalism\BepInEx\plugins\Kapitalism" -Force -Recurse


Copy-Item "..\ModBuild\Kapitalism\BepInEx\*" "..\Game\BepInEx" -Recurse -Force

Start-Process "..\Game\KSP2_x64.exe"