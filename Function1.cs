using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web.Http;
using DurableFunction.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableFunction
{
    public static class Function1
    {
        [FunctionName("Orchestrator")]
        public static async Task<Pedido> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            Pedido pedido = context.GetInput<Pedido>();

            try
            {
                await ValidaPedido(pedido);
                await ProcessaPedido(pedido);
                await FinalizaPedido(pedido);
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }

            return pedido;
        }

       // [FunctionName("ValidaPedido")]
        public static async Task<Pedido> ValidaPedido(Pedido pedido)
        {
            if (pedido == null) pedido.Status = Enum.EStatusProcessamento.ProcessamentoError;
            else if (pedido.Produtos is null) pedido.Status = Enum.EStatusProcessamento.SemProduto;
            else if (pedido.Cliente is null) pedido.Status = Enum.EStatusProcessamento.SemCliente;
            else pedido.Status = Enum.EStatusProcessamento.Processamento;
            return pedido;
        }

       //[FunctionName("ProcessaPedido")]
        public static async Task<Pedido> ProcessaPedido([ActivityTrigger] Pedido pedido)
        {
            if (pedido.Status != Enum.EStatusProcessamento.Processamento) throw new System.Exception("Não foi possivel processar");
            pedido.DataProcessamento = DateTime.Now;
            decimal total = 0;

            foreach(Produto produto in pedido.Produtos)
            {
                total += (produto.Valor * produto.Quantidade);
            }
            pedido.ValorPedido = total;

            return pedido;
        }

        //[FunctionName("FinalizaPedido")]
        public static async Task<Pedido> FinalizaPedido([ActivityTrigger] Pedido pedido)
        {
            pedido.IsEnvio = true;
            return pedido;
        }



        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {

            string data = await req.Content.ReadAsStringAsync();

            Pedido pedido = Newtonsoft.Json.JsonConvert.DeserializeObject<Pedido>(data);

            if(pedido is null)
            {
                return null;
            }
            
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Orchestrator", pedido);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}