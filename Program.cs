using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Data.Sqlite;

namespace StudioMedico
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();
        }
    }

    internal class Server
    {
        // sennò crea il file .db dentro /bin/Debug/net8.0
        private static readonly string _dbConnectionString = $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "database.db")};Mode=ReadWrite";
        public void Start()
        {
            const int port = 8888;
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"Server in ascolto su port {port}");
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private void HandleClient(object? obj)
        {
            if (obj is null)
            {
                return;
            }

            using (TcpClient client = (TcpClient)obj)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Ricevuto: {data}");
                    // Echo del messaggio
                    byte[] response = Encoding.UTF8.GetBytes(data);
                    stream.Write(response, 0, response.Length);
                }
            }
        }

        private void InsertPaziente(string nome, string cognome, string cf, string tel, string email, DateTime nascita, char sesso)
        {
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                SqliteCommand cmd = conn.CreateCommand();
                cmd.CommandText =
                @"
                    INSERT INTO Paziente (Nome, Cognome, CF, Telefono, Mail, DataNascita, Sesso) VALUES
                    ($nome, $cognome, $cf, $tel, $email, $nascita, $sesso)
                ";
                cmd.Parameters.AddWithValue("$nome", nome);
                cmd.Parameters.AddWithValue("$cognome", cognome);
                cmd.Parameters.AddWithValue("$cf", cf);
                cmd.Parameters.AddWithValue("$tel", tel);
                cmd.Parameters.AddWithValue("$email", email);
                cmd.Parameters.AddWithValue("$nascita", nascita);
                cmd.Parameters.AddWithValue("$sesso", sesso);

                cmd.ExecuteNonQuery();
            }
        }
    }
}
