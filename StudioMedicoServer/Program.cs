using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Data.Sqlite;

namespace StudioMedicoServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool insertPlaceholderData = true;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--no-dummy-data")
                    insertPlaceholderData = false;
            }

            Console.WriteLine($"=====\nAvvio server\nInserimento dati placeholder: {insertPlaceholderData}\n=====");

            Server server = new Server(port: 8888, placeholderData: insertPlaceholderData);
            server.Start();
        }
    }

    internal class Server(int port, bool placeholderData)
    {
        // sennò crea il file .db dentro /bin/Debug/net8.0 quando eseguito da dentro vs
        private static readonly string _dbConnectionString = $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "database.db")};";
        public readonly int port = port;
        private readonly bool _insertPlaceholderData = placeholderData;

        public void Start()
        {
            CreaDb();
            if (_insertPlaceholderData)
            {
                InsertPlaceholderData();
            }
            TcpListener server = new TcpListener(IPAddress.Any, this.port);
            server.Start();
            Console.WriteLine($"Server in ascolto su port {this.port}");
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
                    Dictionary<string, string> parsedData = ParseRequest(data);
                    string message;

                    if (!ValidateCredentials(username: parsedData["username"], password: parsedData["password"], ref parsedData))
                    {
                        message = "ERROR\nCredenziali mancanti o non valide.";
                        byte[] res = Encoding.UTF8.GetBytes(message);
                        stream.Write(res, 0, res.Length);
                        return;
                    }

                    switch (parsedData["command"])
                    {
                        case "1":
                            message = VisualizzaAppuntamenti(
                                DateTime.Parse(parsedData["data"]).ToString("yyyy-MM-dd"),
                                Convert.ToInt32(parsedData["medicoId"])
                                );
                            break;
                        case "2":
                            message = StoriaClinica(
                                Convert.ToInt32(parsedData["pazienteId"])
                                );
                            break;
                        case "3":
                            message = InsertVisita(
                                data: parsedData["data"],
                                ora: parsedData["ora"],
                                motivo: parsedData["motivo"],
                                diagnosi: parsedData["diagnosi"],
                                prescrizioni: parsedData["prescrizioni"],
                                medicoId: Convert.ToInt32(parsedData["medicoId"]),
                                pazienteId: Convert.ToInt32(parsedData["pazienteId"])
                                );
                            break;
                        case "4":
                            message = InsertCertificato(
                                data: parsedData["data"],
                                diagnosi: parsedData["diagnosi"],
                                giorniDiMalattia: parsedData["giorniDiMalattia"],
                                medicoId: Convert.ToInt32(parsedData["medicoId"]),
                                pazienteId: Convert.ToInt32(parsedData["pazienteId"])
                                );
                            break;
                        default:
                            message = "ERROR\nNumero comando non valido.";
                            break;
                    }

                    byte[] response = Encoding.UTF8.GetBytes(message);
                    stream.Write(response, 0, response.Length);
                }
            }
        }

        private static Dictionary<string, string> ParseRequest(string request)
        {
            Dictionary<string, string> parsedData = [];

            List<string> data = request.Split('\n').ToList();

            parsedData["command"] = data[0];
            for (int i = 1; i < data.Count; i++)
            {
                string key = data[i].Split(':', count: 2)[0].Trim();
                string value = data[i].Split(':', count: 2)[1].Trim();
                parsedData[key] = value;
            }

            return parsedData;
        }

        private static bool ValidateCredentials(string username, string password, ref Dictionary<string, string> parsedData)
        {
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        @"
                            SELECT medicoId FROM Medico WHERE Username = $username AND Password = $password LIMIT 1;
                        ";
                    cmd.Parameters.AddWithValue("$username", username);
                    cmd.Parameters.AddWithValue("$password", password);
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            parsedData["medicoId"] = reader.GetString(0);
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        private static void CreaDb()
        {
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                        CREATE TABLE IF NOT EXISTS Paziente (
                            Codice INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nome TEXT NOT NULL,
                            Cognome TEXT NOT NULL,
                            CF CHAR(16) NOT NULL UNIQUE,
                            Telefono TEXT,
                            Mail TEXT,
                            DataNascita DATE NOT NULL,
                            Sesso CHAR(1) CHECK(Sesso IN ('M', 'F'))
                        );
                    
                        CREATE TABLE IF NOT EXISTS Medico (
	                        Matricola INTEGER PRIMARY KEY,
	                        Nome TEXT NOT NULL,
	                        Cognome TEXT NOT NULL,
	                        CF CHAR(16) NOT NULL UNIQUE,
	                        Telefono TEXT,
	                        Mail TEXT,
	                        Username TEXT NOT NULL UNIQUE,
	                        Password TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS Appuntamento (
	                        Data DATE NOT NULL,
	                        Ora TIME NOT NULL,
	                        Motivo TEXT NOT NULL,
	                        Tipo CHAR(1) NOT NULL CHECK(Tipo IN ('V', 'C')),
	                        Medico INTEGER NOT NULL,
	                        Paziente INTEGER NOT NULL,
	                        PRIMARY KEY(Data, Ora, Medico),
	                        FOREIGN KEY(Medico) REFERENCES Medico(Matricola) ON DELETE RESTRICT ON UPDATE CASCADE,
	                        FOREIGN KEY(Paziente) REFERENCES Paziente(Codice) ON DELETE RESTRICT ON UPDATE CASCADE
                        );

                        CREATE TABLE IF NOT EXISTS Visita (
	                        Data DATE NOT NULL,
	                        Ora TIME NOT NULL,
	                        Motivo TEXT NOT NULL,
	                        Diagnosi TEXT NOT NULL,
	                        Prescrizioni TEXT,
	                        Medico INTEGER NOT NULL,
	                        Paziente INTEGER NOT NULL,
	                        PRIMARY KEY(Data, Ora, Medico),
	                        FOREIGN KEY(Medico) REFERENCES Medico(Matricola) ON DELETE RESTRICT ON UPDATE CASCADE,
	                        FOREIGN KEY(Paziente) REFERENCES Paziente(Codice) ON DELETE RESTRICT ON UPDATE CASCADE
                        );

                        CREATE TABLE IF NOT EXISTS Certificato (
	                        Data DATE NOT NULL,
	                        Diagnosi TEXT NOT NULL,
	                        Giorni INTEGER NOT NULL CHECK(Giorni > 0),
	                        Medico INTEGER NOT NULL,
	                        Paziente INTEGER NOT NULL,
	                        PRIMARY KEY(Data, Medico, Paziente),
	                        FOREIGN KEY(Medico) REFERENCES Medico(Matricola) ON DELETE RESTRICT ON UPDATE CASCADE,
	                        FOREIGN KEY(Paziente) REFERENCES Paziente(Codice) ON DELETE RESTRICT ON UPDATE CASCADE
                        );
                    ";

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void InsertPlaceholderData()
        {
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                        INSERT OR IGNORE INTO Paziente (Nome, Cognome, CF, Telefono, Mail, DataNascita, Sesso) VALUES
                        ('Tommaso', 'Tonelli', 'TNLTMM06L04H501Z', '3331234567', 'tommaso.iltone@email.com', '2006-07-04', 'M'),
                        ('Laura', 'Bianchi', 'BNCMRA85C45F205Y', '3337654321', 'laura.bianchi@email.com', '1985-03-15', 'F'),
                        ('Marco', 'Verdi', 'VRDMRC90A01H501Z', '3319988776', 'marco.verdi@email.com', '1990-01-01', 'M'),
                        ('Giulia', 'Rossi', 'RSSGLA95B12F205X', '3405566778', 'giulia.rossi@email.com', '1995-02-12', 'F'),
                        ('Federico', 'Bianchi', 'BNCFRC85D22H501Y', '3206677889', 'federico.bianchi@email.com', '1985-04-22', 'M');

                        INSERT OR IGNORE INTO Medico (Matricola, Nome, Cognome, CF, Telefono, Mail, Username, Password) VALUES
                        (1001, 'Gregorio', 'Pescucci', 'PSCGGR06C30AAAAA', '3381234567', 'gregorio.pescucci@email.com', 'gregpesc', 'Pippo123'),
                        (1002, 'Anna', 'Neri', 'NRIANN75D22H501Z', '3397766554', 'anna.neri@email.com', 'aneri', 'password456'),
                        (1003, 'Luca', 'Gialli', 'GLLLCA82E15H501V', '3284433221', 'luca.gialli@email.com', 'lgialli', 'securepass');

                        INSERT OR IGNORE INTO Appuntamento (Data, Ora, Motivo, Tipo, Medico, Paziente) VALUES
                        ('2025-04-10', '09:30', 'Dolore alla gamba', 'V', 1001, 1),
                        ('2025-04-10', '10:30', 'Controllo post-operatorio', 'V', 1001, 3),
                        ('2025-04-10', '11:00', 'Visita di routine', 'C', 1002, 4),
                        ('2025-04-11', '10:00', 'Visita specialistica', 'C', 1002, 2),
                        ('2025-04-11', '11:30', 'Febbre persistente', 'V', 1003, 5);

                        INSERT OR IGNORE INTO Visita (Data, Ora, Motivo, Diagnosi, Prescrizioni, Medico, Paziente) VALUES
                        ('2025-03-15', '14:00', 'Dolore alla gamba', 'Rottura crociato', 'Riposo e riabilitazione', 1001, 1),
                        ('2025-03-15', '15:00', 'Mal di testa frequente', 'Emicrania', 'Analgesico e riposo', 1002, 3),
                        ('2025-03-15', '16:00', 'Tosse persistente', 'Bronchite', 'Antibiotico e riposo', 1003, 4),
                        ('2025-03-16', '15:30', 'Febbre alta', 'Influenza', 'Riposo e antipiretico', 1002, 2),
                        ('2025-03-16', '16:30', 'Affaticamento cronico', 'Anemia', 'Dieta ricca di ferro', 1003, 5);

                        INSERT OR IGNORE INTO Certificato (Data, Diagnosi, Giorni, Medico, Paziente) VALUES
                        ('2025-03-17', 'Influenza', 5, 1002, 2),
                        ('2025-03-17', 'Bronchite', 7, 1003, 4),
                        ('2025-03-18', 'Anemia', 3, 1003, 5),
                        ('2025-03-18', 'Rottura crociato', 30, 1001, 1),
                        ('2025-03-19', 'Emicrania', 2, 1002, 3);
                    ";

                    cmd.ExecuteNonQuery();
                }
            }
        }


        private static string VisualizzaAppuntamenti(string date, int medicoId)
        {
            string response = "OK DATI\n";
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @$"
                        SELECT strftime(%H:%M, Appuntamento.Ora), Appuntamento.Motivo, Appuntamento.Tipo, Paziente.Nome, Paziente.Cognome
                        FROM Appuntamento INNER JOIN Paziente ON Appuntamento.Paziente = Paziente.Codice
                        WHERE strftime(%Y-%m-%d, Appuntamento.Data) = strftime(%Y-%m-%d, $date), Appuntamento.Medico = $medicoId
                        ORDER BY time(Appuntamento.Ora) ASC;
                    ";
                    cmd.Parameters.AddWithValue("$date", date);
                    cmd.Parameters.AddWithValue("$medicoId", medicoId);
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ora = reader.GetString(0);
                            string motivo = reader.GetString(1);
                            string tipo = reader.GetString(2);
                            string nome = reader.GetString(3);
                            string cognome = reader.GetString(4);
                            response += $"ora: {ora}\nmotivo: {motivo}\ntipo: {tipo}\nnome: {nome}\ncognome: {cognome}\n\n\n";
                        }
                        return response;
                    }
                }
            }
        }

        private static string StoriaClinica(int pazienteId)
        {
            string response = "OK DATI\n";
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @$"
                        SELECT strftime(%Y-%m-%d, Visita.Data), Visita.Motivo, Visita.Diagnosi, Visita.Prescrizioni, Medico.Nome, Medico.Cognome
                        FROM Visita INNER JOIN Medico ON Visita.Medico = Medico.Matricola
                        WHERE Visita.Paziente = $pazienteId
                        ORDER BY Visita.Data DESC;
                    ";
                    cmd.Parameters.AddWithValue("$pazienteId", pazienteId);
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string data = reader.GetString(0);
                            string motivo = reader.GetString(1);
                            string diagnosi = reader.GetString(2);
                            string prescrizioni = reader.GetString(3);
                            string nome = reader.GetString(4);
                            string cognome = reader.GetString(5);
                            response += $"data: {data}\nmotivo: {motivo}\ndiagnosi: {diagnosi}\nprescrizioni: {prescrizioni}\nnome: {nome}\ncognome: {cognome}\n\n\n";
                        }
                        return response;
                    }
                }
            }
        }

        private static string InsertVisita(string data, string ora, string motivo, string diagnosi, string prescrizioni, int medicoId, int pazienteId)
        {
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                        INSERT INTO Visita (Data, Ora, Motivo, Diagnosi, Prescrizioni, Medico, Paziente) VALUES
                        (strftime(%Y-%m-%d, $data), strftime(%H:%M, $ora), $motivo, $diagnosi, $prescrizioni, $medicoId, $pazienteId)
                    ";
                    cmd.Parameters.AddWithValue("$data", data);
                    cmd.Parameters.AddWithValue("$ora", ora);
                    cmd.Parameters.AddWithValue("$motivo", motivo);
                    cmd.Parameters.AddWithValue("$diagnosi", diagnosi);
                    cmd.Parameters.AddWithValue("$prescrizioni", prescrizioni);
                    cmd.Parameters.AddWithValue("$medicoId", medicoId);
                    cmd.Parameters.AddWithValue("$pazienteId", pazienteId);

                    cmd.ExecuteNonQuery();
                }
            }
            return "OK\nVisita inserita con successo.";
        }

        private static string InsertCertificato(string data, string diagnosi, string giorniDiMalattia, int medicoId, int pazienteId)
        {
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                        INSERT INTO Visita (Data, Diagnosi, Giorni, Medico, Paziente) VALUES
                        (strftime(%Y-%m-%d, $data), $diagnosi, $giorniDiMalattia, $medicoId, $pazienteId)
                    ";
                    cmd.Parameters.AddWithValue("$data", data);
                    cmd.Parameters.AddWithValue("$diagnosi", diagnosi);
                    cmd.Parameters.AddWithValue("giorniDiMalattia", giorniDiMalattia);
                    cmd.Parameters.AddWithValue("medicoId", medicoId);
                    cmd.Parameters.AddWithValue("pazienteId", pazienteId);

                    cmd.ExecuteNonQuery();
                }
            }
            return "OK\nCertificato inserito con successo.";
        }

        private static string InsertPaziente(string nome, string cognome, string cf, string tel, string email, DateTime nascita, char sesso)
        {
            using (SqliteConnection conn = new SqliteConnection(_dbConnectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
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
            return "OK\nPaziente inserito con successo";
        }
    }
}
