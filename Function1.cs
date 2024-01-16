using DurableFunction.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunction;

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

    public static async Task<Pedido> ValidaPedido(Pedido pedido)
    {
        if (pedido == null) pedido.Status = Enum.EStatusProcessamento.ProcessamentoError;
        else if (pedido.Produtos is null) pedido.Status = Enum.EStatusProcessamento.SemProduto;
        else if (pedido.Cliente is null) pedido.Status = Enum.EStatusProcessamento.SemCliente;
        else pedido.Status = Enum.EStatusProcessamento.Processamento;
        return pedido;
    }

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

    public static async Task<Pedido> FinalizaPedido([ActivityTrigger] Pedido pedido)
    {
        pedido.IsEnvio = true;
        return pedido;
    }

    public static async Task<Enum.EStatusProcessamento> GetStatus(Guid id)
    {
        return Enum.EStatusProcessamento.Processamento;
    }

    [FunctionName("Pedido_HttpStart")]
    public static async Task<HttpResponseMessage> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {

        string data = await req.Content.ReadAsStringAsync();

        Pedido pedido = Newtonsoft.Json.JsonConvert.DeserializeObject<Pedido>(data);
        
        if(pedido is null)
        {
            return null;
        }

        pedido.Id = Guid.NewGuid();
        
        string instanceId = await starter.StartNewAsync("Orchestrator", pedido);

        return new HttpResponseMessage
        {
            Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(pedido), System.Text.Encoding.UTF8, "application/json")
        };
    }

    [FunctionName("GetPedido")]
    public static async Task<HttpResponseMessage> GetPedido(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetPedido")] HttpRequest req,
        ILogger log)
    {
        var id = req.Query["id"];

        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        Enum.EStatusProcessamento status = await GetStatus(Guid.Parse(id));
        var processamento = new 
        { 
            id,
            status
        };

        return new HttpResponseMessage 
        {
            Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(processamento), System.Text.Encoding.UTF8, "application/json")
        };
            
    }

}