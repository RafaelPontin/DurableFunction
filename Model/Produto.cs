using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunction.Model;

public class Produto : Base
{
    public string Nome { get; set; }
    public string Descricao { get; set;}
    public decimal Valor {  get; set; }
    public int Quantidade { get; set; }
}
