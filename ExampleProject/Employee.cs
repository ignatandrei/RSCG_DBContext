using System;
using System.Collections.Generic;

namespace SampleDatabase;

public partial class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Iddepartment { get; set; }

    public virtual Department IddepartmentNavigation { get; set; } = null!;
}
