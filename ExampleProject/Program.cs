using SampleDatabase;

EmpContext emp = new EmpContext();
Console.WriteLine(emp.Exists_Department());
Console.WriteLine(emp.Exists_AllDBSets());
foreach(var item in emp.Problem_DBSets())
{
    Console.WriteLine(item);
}