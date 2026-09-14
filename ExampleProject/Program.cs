using SampleDatabase;

EmpContext emp = new EmpContext();
Console.WriteLine(emp.Exists_Department());
Console.WriteLine(emp.Exists_AllDBSets());