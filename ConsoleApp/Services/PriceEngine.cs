﻿using ConsoleApp1.Builders;
using ConsoleApp1.Factories;
using ConsoleApp1.Model;
using ConsoleApp1.QuotationSystems;
using ConsoleApp1.Validators;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Services
{

    public class PriceEngine
    {
        private IQuotationSystemFactory _quotationSystemFactory { get; set; }
        private IPriceResonseBuilder _priceResonseBuilder { get; set; }
        private IPriceRequestValidator _priceRequestValidator { get; set; }
        public PriceEngine() : this(new QuotationSystemFactory(new PriceEngineConfigurations(), new ExternalQuoteRequestResponseBuilder()), new PriceResonseBuilder(), new PriceRequestValidator())
        { 
        }

        public PriceEngine(IQuotationSystemFactory quotationSystemFactory, IPriceResonseBuilder priceResonseBuilder, IPriceRequestValidator priceRequestValidator)
        {
            _quotationSystemFactory = quotationSystemFactory;
            _priceResonseBuilder = priceResonseBuilder;
            _priceRequestValidator = priceRequestValidator;
        }


        //pass request with risk data with details of a gadget, return the best price retrieved from 3 external quotation engines
        public async Task<PriceResponse> GetPriceAsync(PriceRequest request)
        {
            //initialise return variables
            var priceResponse = new PriceResponse() { InsurerName = String.Empty, Tax = 0, Price = -1 };

            //validation
            priceResponse.ErrorMessage = _priceRequestValidator.Validate(request);

            if (priceResponse.ErrorMessage.Count<=0)
            {
                //now call 3 external system and get the best price 
                var quotationSystems = _quotationSystemFactory.GenerateQutationSystems(request);

                //get quote from each quation system
                var priceQuoteTasks = new List<Task<PriceResponse>>();
                foreach (var quotationSystem in quotationSystems)
                {
                    priceQuoteTasks.Add(quotationSystem.GetPriceAsync(request));
                }

                await Task.WhenAll(priceQuoteTasks);

                priceResponse = _priceResonseBuilder.BuildResponse(priceQuoteTasks.Where(t => t.IsCompleted).Select(t => t.Result)?.ToList());
            } 

            return priceResponse;
        }
    }
}
