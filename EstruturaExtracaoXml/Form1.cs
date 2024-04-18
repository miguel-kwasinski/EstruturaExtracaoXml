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
                await ExtrairInformacoesArquivoAsync(caminhoDoArquivo);
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

        private async Task ExtrairInformacoesArquivoAsync(string caminhoDoArquivo)
        {
            try
            {
                XDocument xDoc = XDocument.Load(caminhoDoArquivo);
                string tipoEvento = await Task.Run(() => IdentificaEvento.ObterNomeEvento(xDoc));

                if (!string.IsNullOrEmpty(tipoEvento))
                {
                    dataGridExtracao.SelectedRows[0].Cells["Situacao"].Value = "Extraido";
                    string versao = await IdentificaEvento.IdentificarVersaoAsync(xDoc, tipoEvento);

                    // Chama o m�todo ExtrairXMLParaLista para obter os n�s XML
                    List<ExtratorEvento.XMLNode> nodeList = await ExtratorEvento.ExtrairXMLParaListaAsync(xDoc.Root).ToListAsync();
                    // Agora voc� pode usar a lista 'nodeList' conforme necess�rio
                    // Por exemplo, exibir os n�s em uma caixa de mensagem:
                    string nodesText = string.Join("\n", nodeList.Select(node => $"{node.Name}: {node.Value}"));
                    MessageBox.Show($"N�s XML extra�dos:\n{nodesText}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao extrair informa��es do arquivo: {ex.Message}");
            }
        }
    }
}
