using System;
using System.Collections.Generic;

namespace SampleDatabase;

public partial class Department
{
    public Department()
    {
        Employee = new HashSet<Employee>();
        Name = string.Empty;
    }

    public int Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<Employee> Employee { get; set; }
}
