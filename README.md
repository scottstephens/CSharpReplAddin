C# REPL Addin

Debugging on Windows in Visual Studio
* Make sure the parent directory of the CSharpReplAddin source directory has a directory called "monodevelop" in which the MonoDevelop source code is checked into and built
* Use the DebugWindows/AnyCpu configuration
* In the CSharpReplAddin project's Debug configuration, set the startup program to ..\monodevelop\main\build\bin\MonoDevelop.exe, and the working directory to C:\Users\rock361\Development\thirdpartywork\monodevelop\main\build\bin\
* In the MonoDevelop main/build/bin directory, edit the MonoDevelop.exe.addins file to include <Directory include-subdirs="false">..\..\..\..\CSharpReplAddin\CSharpReplAddin\bin\Debug</Directory> inside the <Addins> section