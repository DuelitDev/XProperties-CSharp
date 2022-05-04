# XProperties
XProperties is a .properties file parser library, which was written to read/write .properties files in languages other than Java.  
## Install
`Install-Package DuelitDev.XProperties`  
## Documentation
[XProperties Wiki](https://github.com/DuelitDev/XProperties-CSharp/wiki) 
## Example
```c#
// example.cs
// See https://aka.ms/new-console-template for more information

using XProperties;

var prop = new Properties();
prop.Load("example.properties");
Console.WriteLine(prop["example"]);
```
## Copyright
Copyright 2022. Kim-Jaeyun all rights reserved.  
## License
[LGPL-2.1 License](https://github.com/DuelitDev/XProperties/blob/master/LICENSE)  