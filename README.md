# StudioMedico

## Valori per testing

Username medico: 1  
Password medico: porciatti  
Data con visite: 2025-04-10  
Codice fiscale paziente: TNLTMM06L04H501Z

## Client

args:

-   `--ip-address <str: ip_address>` Indirizzo ip del server (Default: localhost)
-   `--port <int: port>` Porta del server (Default: 8888)

## Server

File db e log dentro `/StudioMedicoServer`.

args:

-   `--port <int: port>` Porta si cui stare in ascolto (Default: 8888)

-   `--no-placeholder-data` Non inserire dati placeholder nel database

## Protocollo

### Formato richiesta (parametri se richiesti dal comando, in qualsiasi ordine)

```
<numero del comando>
username: <username>
password: <password>
<parametro 1>: <valore 1>
<parametro 2>: <valore 2>
<parametro n>: <valore n>
```

### Formato risposta

Se la risposta prevede dei dati (dati diversi separati da due righe vuote)

```
OK DATI
<parametro 1>: <valore 1>
<parametro n>: <valore n>


<parametro 1>: <valore 1>
<parametro n>: <valore n>
```

oppure se non prevede dati

```
OK
<messaggio informativo>
```

oppure in tutti i casi di errore

```
ERROR
<messaggio informativo>
```
