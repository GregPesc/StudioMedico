using System.Net.Sockets;
using System.Text;

namespace StudioMedicoClient
{
    class Client
    {
        static void Main(string[] args)
        {
            string serverIp = "localhost";
            int port = 8888;

            try
            {
                using (TcpClient client = new TcpClient(serverIp, port))
                using (NetworkStream stream = client.GetStream())
                {
                    Console.WriteLine("Connesso al server.");

                    Console.Write("Username: ");
                    string username = Console.ReadLine();
                    Console.Write("Password: ");
                    string password = Console.ReadLine();

                    while (true)
                    {
                        Console.WriteLine("Scegli un comando:");
                        Console.WriteLine("1 - Visualizza appuntamenti");
                        Console.WriteLine("2 - Storia clinica");
                        Console.WriteLine("3 - Inserisci visita");
                        Console.WriteLine("4 - Inserisci certificato");
                        Console.WriteLine("5 - Esci");
                        Console.Write("Opzione: ");
                        string command = Console.ReadLine();

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
                            Console.Write("ID Paziente: ");
                            string pazienteId = Console.ReadLine();
                            request.AppendLine($"pazienteId: {pazienteId}");
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
                            Console.Write("ID Paziente: ");
                            string pazienteId = Console.ReadLine();

                            request.AppendLine($"data: {data}");
                            request.AppendLine($"ora: {ora}");
                            request.AppendLine($"motivo: {motivo}");
                            request.AppendLine($"diagnosi: {diagnosi}");
                            request.AppendLine($"prescrizioni: {prescrizioni}");
                            request.AppendLine($"pazienteId: {pazienteId}");
                        }
                        else if (command == "4")
                        {
                            Console.Write("Data (YYYY-MM-DD): ");
                            string data = Console.ReadLine();
                            Console.Write("Diagnosi: ");
                            string diagnosi = Console.ReadLine();
                            Console.Write("Giorni di Malattia: ");
                            string giorniDiMalattia = Console.ReadLine();
                            Console.Write("ID Paziente: ");
                            string pazienteId = Console.ReadLine();

                            request.AppendLine($"data: {data}");
                            request.AppendLine($"diagnosi: {diagnosi}");
                            request.AppendLine($"giorniDiMalattia: {giorniDiMalattia}");
                            request.AppendLine($"pazienteId: {pazienteId}");
                        }

                        byte[] requestData = Encoding.UTF8.GetBytes(request.ToString());
                        stream.Write(requestData, 0, requestData.Length);

                        byte[] responseData = new byte[1024];
                        int bytesRead = stream.Read(responseData, 0, responseData.Length);
                        string response = Encoding.UTF8.GetString(responseData, 0, bytesRead);

                        Console.WriteLine("Risposta del server:");
                        Console.WriteLine(response);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Errore: {e.Message}");
            }
        }
    }
}