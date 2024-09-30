using System;
using JsonSharp2;

class Test {
  public string String = "TestString";
  public double Number = 3.14159265;
  public int[] Array = new int[] { 1, 2, 3, 4, 5 };
}

class MainClass {
  public static void Main (string[] args) {
    Console.WriteLine("Serialization: ");

	string serialized = Json2.Stringify(new Test());

    Console.WriteLine(serialized);

	Console.WriteLine();

    dynamic json = Json2.Parse(serialized);

	try {
    Console.WriteLine(json.Ds);
	}
	catch {
		Console.WriteLine("Non existant field");
	}
	  
	Console.WriteLine(json.Number);
	
	Console.WriteLine("Array: ");

	foreach (string str in json.Array) {
		Console.WriteLine(str);
	}
  }
}