using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace EstruturaExtracaoXml
{
    public partial class Form1 : Form
    {
        private DataTable dataArquivos = new DataTable();

        public Form1()
        {
            InitializeComponent();
            InicializarDataTable();
        }

        private void InicializarDataTable()
        {
            dataArquivos.Columns.Add("CaminhoArquivo");
            dataArquivos.Columns.Add("Situacao");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataArquivos.Clear();
            string caminho_raiz = Path.Combine(Environment.CurrentDirectory, "Xml");
            DirectoryInfo diretorio = new DirectoryInfo(caminho_raiz);
            FileInfo[] Arquivos = diretorio.GetFiles("*.*");

            foreach (FileInfo fileinfo in Arquivos)
            {
                DataRow colunaArquivo = dataArquivos.NewRow();
                colunaArquivo[0] = Path.Combine(caminho_raiz, fileinfo.Name);
                colunaArquivo[1] = "Pendente";
                dataArquivos.Rows.Add(colunaArquivo);
            }

            dataGridExtracao.DataSource = dataArquivos;
        }

        private async void buttonExtract_Click(object sender, EventArgs e)
        {
            if (dataGridExtracao.SelectedRows.Count > 0 && dataGridExtracao.SelectedRows[0].Cells["Situacao"].Value.ToString() != "Extraido")
            {
                string caminhoDoArquivo = dataGridExtracao.SelectedRows[0].Cells["CaminhoArquivo"].Value.ToString();
                await ExtrairInformacoesXMLAsync(caminhoDoArquivo);
            }
            else
            {
                if (dataGridExtracao.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Nenhuma linha selecionada.");
                }
                else
                {
                    MessageBox.Show("Arquivo j� extra�do");
                }
            }
        }

        private async Task ExtrairInformacoesXMLAsync(string caminhoDoArquivo)
        {
            try
            {
                XDocument xDoc = XDocument.Load(caminhoDoArquivo);
                IdentificaEvento.EventoInfo eventoInfo = new IdentificaEvento.EventoInfo();
                eventoInfo.TipoEvento = await Task.Run(() => IdentificaEvento.ObterNomeEvento(xDoc));

                if (!string.IsNullOrEmpty(eventoInfo.TipoEvento))
                {
                    dataGridExtracao.SelectedRows[0].Cells["Situacao"].Value = "Extraido";
                    eventoInfo.Versao = await IdentificaEvento.IdentificarVersaoAsync(xDoc, eventoInfo.TipoEvento);

                    // Chama o m�todo ExtrairXMLParaLista para obter os n�s XML
                    List<ExtratorEventoGeral.XMLNode> nodeList = await ExtratorEventoGeral.ExtrairXMLParaListaAsync(xDoc.Root).ToListAsync();

                    //Cria a lista de elementos que ir�o ser extraidos 
                    List<string> nodeNames = await IdentificaEvento.NomesDesejadosPorEventoAsync(eventoInfo);

                    // Chama o m�todo FiltrarPorNomeDoNo para filtrar os n�s pelo nome
                    List<ExtratorEventoGeral.XMLNode> nodeListFiltrados = ExtratorEventoGeral.FiltrarPorNomeDoNo(nodeList, nodeNames).ToList();

                    /*demonstra��o de utiliza��o dos valores da lista*/

                    // Busca o nome do objeto desejado pelo usu�rio (substitua por como voc� obt�m esse nome)
                    string nomeDoObjeto = "ideEvento";

                    // Filtra a lista com base no nome do objeto
                    List<ExtratorEventoGeral.XMLNode> objetosEncontrados = nodeListFiltrados
                        .Where(node => node.Name.Equals(nomeDoObjeto, StringComparison.OrdinalIgnoreCase))
                        .ToList(); // Obt�m todos os elementos encontrados (pode haver v�rios)

                    // Verifica se algum objeto foi encontrado
                    if (objetosEncontrados.Count > 0)
                    {
                        // Mensagem inicial para indicar o in�cio da listagem
                        MessageBox.Show($"Objetos encontrados com o nome '{nomeDoObjeto}':");

                        // Loop para exibir cada objeto encontrado
                        for (int i = 0; i < objetosEncontrados.Count; i++)
                        {
                            ExtratorEventoGeral.XMLNode objetoAtual = objetosEncontrados[i];

                            // Mensagem exibindo o nome e valor do objeto
                            MessageBox.Show($"Objeto {i + 1}:\nNome: {objetoAtual.Name}\nValor: {objetoAtual.Value}");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Objeto '{nomeDoObjeto}' n�o encontrado na lista.");
                    }


                    /*Aqui adicionar a l�gica para o insert no banco de dados � partir da lista*/

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao extrair informa��es do arquivo: {ex.Message}");
            }
        }
    }
}