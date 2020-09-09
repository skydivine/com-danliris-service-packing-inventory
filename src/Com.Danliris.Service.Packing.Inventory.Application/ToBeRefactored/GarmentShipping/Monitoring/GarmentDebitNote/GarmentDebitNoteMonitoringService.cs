﻿using Com.Danliris.Service.Packing.Inventory.Application.Utilities;
using Com.Danliris.Service.Packing.Inventory.Data.Models.Garmentshipping.ShippingNote;
using Com.Danliris.Service.Packing.Inventory.Infrastructure.IdentityProvider;
using Com.Danliris.Service.Packing.Inventory.Infrastructure.Repositories.GarmentShipping.ShippingNote;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Com.Danliris.Service.Packing.Inventory.Application.ToBeRefactored.GarmentShipping.Monitoring.GarmentDebitNote
{
    public class GarmentDebitNoteMonitoringService : IGarmentDebitNoteMonitoringService
    {
        private readonly IGarmentShippingNoteRepository repository;
        private readonly IGarmentShippingNoteItemRepository itemrepository;
    
        private readonly IIdentityProvider _identityProvider;

        public GarmentDebitNoteMonitoringService(IServiceProvider serviceProvider)
        {
            repository = serviceProvider.GetService<IGarmentShippingNoteRepository>();
            itemrepository = serviceProvider.GetService<IGarmentShippingNoteItemRepository>();
            _identityProvider = serviceProvider.GetService<IIdentityProvider>();
        }

       public IQueryable<GarmentDebitNoteMonitoringViewModel> GetData(string buyerAgent, DateTime? dateFrom, DateTime? dateTo, int offset)
        {
            var query = repository.ReadAll();
            var queryitem = itemrepository.ReadAll();
       
            if (!string.IsNullOrWhiteSpace(buyerAgent))
            {
                query = query.Where(w => w.BuyerCode == buyerAgent);
            }

            DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            query = query.Where(w => w.Date.AddHours(offset).Date >= DateFrom.Date && w.Date.AddHours(offset).Date <= DateTo.Date);

            query = query.OrderBy(w => w.BuyerCode).ThenBy(b => b.NoteNo);
            
            var newQ = (from a in query
                        join b in queryitem on a.Id equals b.ShippingNoteId
                        where a.NoteType == GarmentShippingNoteTypeEnum.DN

                        select new GarmentDebitNoteMonitoringViewModel
                        {
                            DNNo = a.NoteNo,
                            DNDate = a.Date,
                            BuyerCode = a.BuyerCode,
                            BuyerName = a.BuyerName,
                            Description = b.Description,
                            CurrencyCode = b.CurrencyCode,
                            Amount = b.Amount,
                          });
            return newQ;
        }

        public List<GarmentDebitNoteMonitoringViewModel> GetReportData(string buyerAgent, DateTime? dateFrom, DateTime? dateTo, int offset)
        {
            var Query = GetData(buyerAgent, dateFrom, dateTo, offset);
            Query = Query.OrderBy(b => b.BuyerCode).ThenBy(b => b.DNNo);
            return Query.ToList();
        }

        public MemoryStream GenerateExcel(string buyerAgent, DateTime? dateFrom, DateTime? dateTo, int offset)
       {

            var Query = GetData(buyerAgent, dateFrom, dateTo, offset);
            DataTable result = new DataTable();

            result.Columns.Add(new DataColumn() { ColumnName = "No", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "No Nota Debit", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tanggal Nota Debit", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "Kode Buyer", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama Buyer", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "K e t e r a n g a n", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "Mata Uang", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "J u m l a h", DataType = typeof(string) });
            
            if (Query.ToArray().Count() == 0)
                result.Rows.Add("", "", "", "", "", "", "", "");
            else
            {
                int index = 0;
                foreach (var d in Query)
                {
                    index++;

                    string NoteDate = d.DNDate == new DateTime(1970, 1, 1) ? "-" : d.DNDate.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                 
                    string Amnt = string.Format("{0:N2}", d.Amount);
                       
                    result.Rows.Add(index, d.DNNo, NoteDate, d.BuyerCode, d.BuyerName, d.Description, d.CurrencyCode, Amnt);
                }
            }
          
            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Sheet1") }, true);
        }
    }
}
