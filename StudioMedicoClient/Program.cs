using System.Net.Sockets;
using System.Text;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace StudioMedicoClient
{
    class Client
    {
        static void Main(string[] args)
        {
            string serverIp = "localhost";
            int port = 8888;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--ip-address" && i + 1 < args.Length)
                    serverIp = args[i + 1];
                if (args[i] == "--port" && i + 1 < args.Length)
                {
                    try
                    {
                        port = Convert.ToInt32(args[i + 1]);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Numero porta non valido.");
                        return;
                    }
                }

            }

            try
            {
                using (TcpClient client = new TcpClient(serverIp, port))
                using (SslStream stream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                {
                    // Autenticazione del client
                    stream.AuthenticateAsClient(serverIp, null, SslProtocols.Tls12 | SslProtocols.Tls13, true);
                    Console.WriteLine($"Connessione sicura stabilita {stream.IsAuthenticated}");

                    Console.Write("Username: ");
                    string username = Console.ReadLine();
                    Console.Write("Password: ");
                    string password = Console.ReadLine();

                    while (true)
                    {

                        string command;
                        do
                        {
                            Console.Clear();
                            Console.WriteLine("Scegli un comando:");
                            Console.WriteLine("1 - Visualizza appuntamenti");
                            Console.WriteLine("2 - Storia clinica");
                            Console.WriteLine("3 - Inserisci visita");
                            Console.WriteLine("4 - Inserisci certificato");
                            Console.WriteLine("5 - Esci");
                            Console.Write("Opzione: ");
                            command = Console.ReadLine();
                        }
                        while ((command is null) || (command == ""));

                        Console.Clear();

                        if (command == "5")
                            break;

                        StringBuilder request = new StringBuilder();
                        request.AppendLine(command);
                        request.AppendLine($"username: {username}");
                        request.AppendLine($"password: {password}");

                        if (command == "1")
                        {
                            Console.Write("Data (YYYY-MM-DD): ");
                            string data = Console.ReadLine();
                            request.AppendLine($"data: {data}");
                        }
                        else if (command == "2")
                        {
                            Console.Write("Codice fiscale paziente: ");
                            string cfPaziente = Console.ReadLine();
                            request.AppendLine($"cfPaziente: {cfPaziente}");
                        }
                        else if (command == "3")
                        {
                            Console.Write("Data (YYYY-MM-DD): ");
                            string data = Console.ReadLine();
                            Console.Write("Ora (HH:MM): ");
                            string ora = Console.ReadLine();
                            Console.Write("Motivo: ");
                            string motivo = Console.ReadLine();
                            Console.Write("Diagnosi: ");
                            string diagnosi = Console.ReadLine();
                            Console.Write("Prescrizioni: ");
                            string prescrizioni = Console.ReadLine();
                            Console.Write("Codice fiscale paziente: ");
                            string cfPaziente = Console.ReadLine();

                            request.AppendLine($"data: {data}");
                            request.AppendLine($"ora: {ora}");
                            request.AppendLine($"motivo: {motivo}");
                            request.AppendLine($"diagnosi: {diagnosi}");
                            request.AppendLine($"prescrizioni: {prescrizioni}");
                            request.AppendLine($"cfPaziente: {cfPaziente}");
                        }
                        else if (command == "4")
                        {
                            Console.Write("Data (YYYY-MM-DD): ");
                            string data = Console.ReadLine();
                            Console.Write("Diagnosi: ");
                            string diagnosi = Console.ReadLine();
                            Console.Write("Giorni di Malattia: ");
                            string giorniDiMalattia = Console.ReadLine();
                            Console.Write("Codice fiscale paziente: ");
                            string cfPaziente = Console.ReadLine();

                            request.AppendLine($"data: {data}");
                            request.AppendLine($"diagnosi: {diagnosi}");
                            request.AppendLine($"giorniDiMalattia: {giorniDiMalattia}");
                            request.AppendLine($"cfPaziente: {cfPaziente}");
                        }

                        byte[] requestData = Encoding.UTF8.GetBytes(request.ToString());
                        stream.Write(requestData, 0, requestData.Length);

                        byte[] responseData = new byte[1024];
                        int bytesRead = stream.Read(responseData, 0, responseData.Length);
                        string response = Encoding.UTF8.GetString(responseData, 0, bytesRead);

                        Console.Clear();
                        if (response.StartsWith("ERROR"))
                        {
                            Console.WriteLine(response);
                        }
                        else
                        {
                            Console.WriteLine(response.Split('\n', count: 2)[1].Trim());
                        }

                        Console.Write("\n\nPremi un tasto per tornare al menù...");
                        Console.ReadKey();
                        Console.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Errore:\n{e.Message}");
            }
        }
        // Metodo per validare il certificato del server (qui è permissivo solo per esempio)
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            Console.WriteLine($"Errore certificato: {sslPolicyErrors}");
            // ATTENZIONE: per scopi dimostrativi consentiamo sempre.
            // NON usare in produzione!
            return true;
        }
    }

}