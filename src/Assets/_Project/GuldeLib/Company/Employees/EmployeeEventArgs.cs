using System;

namespace GuldeLib.Company.Employees
{
    public class EmployeeEventArgs : EventArgs
    {
        public EmployeeComponent Employee { get; }

        public EmployeeEventArgs(EmployeeComponent employee)
        {
            Employee = employee;
        }
    }
}