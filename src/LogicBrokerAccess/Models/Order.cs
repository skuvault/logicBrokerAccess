﻿using System;
using System.Collections.Generic;

namespace LogicBrokerAccess.Models
{
	public class Order
	{
		public string OrderNumber { get; set; }
		public DateTime DocumentDate { get; set; }	
		public string LogicBrokerKey { get; set; }
		public IEnumerable< OrderLine > OrderLines { get; set; }
		public IEnumerable< LogicBrokerOrderTax > Taxes { get; set; }
		public string ShippingCarrier { get; set; }
		public string ShippingClass { get; set; }
		public LogicBrokerShipToAddress ShipToAddress { get; set; }
		public decimal TotalAmount { get; set; }
		public LogicBrokerOrderStatusEnum StatusCode { get; set; }
		public string Note { get; set; }
		public IEnumerable< LogicBrokerOrderDiscount > Discounts { get; set; }
	}
}
