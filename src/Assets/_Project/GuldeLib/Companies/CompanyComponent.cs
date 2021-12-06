using System;
using System.Collections;
using System.Collections.Generic;
using GuldeLib.Builders;
using GuldeLib.Companies.Carts;
using GuldeLib.Companies.Employees;
using GuldeLib.Economy;
using GuldeLib.Entities;
using GuldeLib.Factories;
using GuldeLib.Maps;
using GuldeLib.Producing;
using GuldeLib.Timing;
using MonoExtensions.Runtime;
using MonoLogger.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GuldeLib.Companies
{
    /// <summary>
    /// Provides information and behavior for companies.
    /// </summary>
    [RequireComponent(typeof(LocationComponent))]
    public class CompanyComponent : SerializedMonoBehaviour
    {
        /// <summary>
        /// Gets or sets the cost of hiring emloyees.
        /// </summary>
        [ShowInInspector]
        [BoxGroup("Settings")]
        public int HiringCost { get; set; }

        /// <summary>
        /// Gets or sets the cost of hiring carts.
        /// </summary>
        [ShowInInspector]
        [BoxGroup("Settings")]
        public int CartCost { get; set; }

        /// <summary>
        /// Gets or sets the cost of wages per hour worked per employee.
        /// </summary>
        [ShowInInspector]
        [BoxGroup("Settings")]
        public float WagePerHour { get; set; }

        /// <summary>
        /// Gets or sets the <see cref = "WealthComponent">Owner</see> of the company.
        /// </summary>
        [ShowInInspector]
        [BoxGroup("Info")]
        public WealthComponent Owner { get; set; }

        /// <summary>
        /// Gets or sets the <see cref = "EmployeeComponent">Employees</see> of the company.
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        [BoxGroup("Info")]
        public HashSet<EmployeeComponent> Employees { get; set; } = new HashSet<EmployeeComponent>();

        /// <summary>
        /// Gets or sets the <see cref = "CartComponent">Carts</see> of the company.
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        [BoxGroup("Info")]
        public HashSet<CartComponent> Carts { get; set; } = new HashSet<CartComponent>();

        /// <summary>
        /// Gets the <see cref = "LocationComponent">Location</see> of the company.
        /// </summary>
        [ShowInInspector]
        [FoldoutGroup("Debug")]
        public LocationComponent Location => this.GetCachedComponent<LocationComponent>();

        /// <summary>
        /// Gets the <see cref = "ProductionComponent">ProductionComponent</see> of the company.
        /// </summary>
        [ShowInInspector]
        [FoldoutGroup("Debug")]
        public ProductionComponent Production => this.GetCachedComponent<ProductionComponent>();

        /// <summary>
        /// Gets the <see cref = "ProductionComponent">ProductionComponent</see> of the company.
        /// </summary>
        [ShowInInspector]
        [FoldoutGroup("Debug")]
        public AssignmentComponent Assignment => this.GetCachedComponent<AssignmentComponent>();

        /// <summary>
        /// Gets the <see cref = "ExchangeComponent">ExchangeComponent</see> of the company.
        /// </summary>
        [ShowInInspector]
        [FoldoutGroup("Debug")]
        public ExchangeComponent Exchange => this.GetCachedComponent<ExchangeComponent>();

        /// <summary>
        /// Gets the <see cref = "ProductionRegistryComponent">ProductionRegistry</see> of the company.
        /// </summary>
        [ShowInInspector]
        [FoldoutGroup("Debug")]
        public EntityRegistryComponent EntityRegistry => this.GetCachedComponent<EntityRegistryComponent>();

        /// <summary>
        /// Gets the <see cref = "MasterComponent">Master</see> of the company.
        /// </summary>
        [ShowInInspector]
        [FoldoutGroup("Debug")]
        public MasterComponent Master => this.GetCachedComponent<MasterComponent>();

        /// <summary>
        /// Invoked after an employee has arrived at the company.
        /// </summary>
        public event EventHandler<EmployeeArrivedEventArgs> EmployeeArrived;

        /// <summary>
        /// Invoked after an employee has left the company.
        /// </summary>
        public event EventHandler<EmployeeLeftEventArgs> EmployeeLeft;

        /// <summary>
        /// Invoked after a cart has arrived at the company.
        /// </summary>
        public event EventHandler<CartArrivedEventArgs> CartArrived;

        /// <summary>
        /// Invoked after a cart has left the company.
        /// </summary>
        public event EventHandler<CartLeftEventArgs> CartLeft;

        /// <summary>
        /// Invoked after an employee has been hired by the company.
        /// </summary>
        public event EventHandler<EmployeeHiredEventArgs> EmployeeHired;

        /// <summary>
        /// Invoked after a new cart has been hired by the company.
        /// </summary>
        public event EventHandler<CartHiredEventArgs> CartHired;

        /// <summary>
        /// Invoked after the wages for an hour were paid.
        /// </summary>
        public event EventHandler<WagePaidEventArgs> WagePaid;

        /// <summary>
        /// Gets whether a given employee is employed by the company.
        /// </summary>
        /// <param name="employee">The employee in question.</param>
        /// <returns></returns>
        public bool IsEmployed(EmployeeComponent employee) => Employees.Contains(employee);

        /// <summary>
        /// Gets whether a given employee is at the company.
        /// </summary>
        /// <param name="employee">The employee in question.</param>
        /// <returns></returns>
        public bool IsAvailable(EmployeeComponent employee) => employee && Location.EntityRegistry.IsRegistered(employee.Entity);

        /// <summary>
        /// Gets wether a given cart is employed by the company.
        /// </summary>
        /// <param name="cart">The cart in question.</param>
        /// <returns></returns>
        public bool IsEmployed(CartComponent cart) => Carts.Contains(cart);

        /// <summary>
        /// Gets whether a give cart is at the company.
        /// </summary>
        /// <param name="cart">The cart in question.</param>
        /// <returns></returns>
        public bool IsAvailable(CartComponent cart) => Location.EntityRegistry.IsRegistered(cart.Entity);

        void Awake()
        {
            this.Log("Company initializing");
        }

        void Start()
        {
            Location.EntityArrived += OnEntityArrived;
            Location.EntityLeft += OnEntityLeft;
            if (Locator.Time) Locator.Time.WorkingHourTicked += OnWorkingHourTicked;
        }

        /// <summary>
        /// Hires a new employee.
        /// </summary>
        public void HireEmployee()
        {
            this.Log("Company is hiring employee");

            var employee = ScriptableObject.CreateInstance<Employee>();

            var employeeFactory = new EmployeeFactory();
            var employeeObject = employeeFactory.Create(employee);
            var employeeComponent = employeeObject.GetComponent<EmployeeComponent>();

            employeeComponent.SetCompany(this);
            Employees.Add(employeeComponent);

            EmployeeHired?.Invoke(this, new EmployeeHiredEventArgs(employeeComponent, HiringCost));
        }

        /// <summary>
        /// Hires a new cart.
        /// </summary>
        /// <param name="cartType"></param>
        public void HireCart(CartType cartType = CartType.Small)
        {
            this.Log("Company is hiring cart");

            var cart = ScriptableObject.CreateInstance<Cart>();
            cart.CartType = cartType;

            var cartFactory = new CartFactory();
            var cartObject = cartFactory.Create(cart);
            var cartComponent = cartObject.GetComponent<CartComponent>();

            cartComponent.SetCompany(this);
            Carts.Add(cartComponent);

            CartHired?.Invoke(this, new CartHiredEventArgs(cartComponent, CartCost));
        }

        /// <summary>
        /// Callback for the <see cref = "LocationComponent.EntityArrived">EntityArrived</see> event
        /// of the company's <see cref = "LocationComponent">LocationComponent</see><br/>
        /// Invokes the <see cref = "CartArrived">CartArrived</see> or <see cref = "EmployeeArrived">EmployeeArrived</see> event.
        /// </summary>
        void OnEntityArrived(object sender, EntityEventArgs e)
        {
            var employee = e.Entity.GetComponent<EmployeeComponent>();
            var cart = e.Entity.GetComponent<CartComponent>();

            if (IsEmployed(employee))
            {
                this.Log($"Employee {employee} arrived at company");
                EmployeeArrived?.Invoke(this, new EmployeeArrivedEventArgs(employee));
            }

            if (IsEmployed(cart))
            {
                this.Log($"Cart {cart} arrived at company");
                CartArrived?.Invoke(this, new CartArrivedEventArgs(cart));
            }
        }

        /// <summary>
        /// Callback for the <see cref = "LocationComponent.EntityLeft">EntityLeft</see> event
        /// of the company's <see cref = "LocationComponent">LocationComponent</see><br/>
        /// Invokes the <see cref = "CartLeft">CartLeft</see> or <see cref = "EmployeeLeft">EmployeeLeft</see> event.
        /// </summary>
        void OnEntityLeft(object sender, EntityEventArgs e)
        {
            var employee = e.Entity.GetComponent<EmployeeComponent>();
            var cart = e.Entity.GetComponent<CartComponent>();

            if (IsEmployed(employee))
            {
                this.Log($"Employee {employee} left company");
                EmployeeLeft?.Invoke(this, new EmployeeLeftEventArgs(employee));
            }

            if (IsEmployed(cart))
            {
                this.Log($"Cart {cart} left company");
                CartLeft?.Invoke(this, new CartLeftEventArgs(cart));
            }
        }

        /// <summary>
        /// Callback for the <see cref = "TimeComponent.WorkingHourTicked">WorkingHourTicked</see> event
        /// of the <see cref = "TimeComponent">TimeComponent</see>.<br/>
        /// Pays the worker's wages.<br/>
        /// Invokes the <see cref = "WagePaid">WagePaid</see> event.
        /// </summary>
        void OnWorkingHourTicked(object sender, TimeEventArgs e)
        {
            var totalWage = Employees.Count * WagePerHour;

            this.Log($"Company billed wages {totalWage}");
            WagePaid?.Invoke(this, new WagePaidEventArgs(totalWage));
        }
    }
}