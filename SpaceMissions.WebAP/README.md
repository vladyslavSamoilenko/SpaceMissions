# Space Missions App <br>
Space Missions App – RESTful API do zarządzania misjami kosmicznymi i rakietami.<br>
### Skład zespołu: Shcherbakov Andrii, Samoilenko Vladyslav <br>
### Krótki opis funkcji programu

### Aplikacja umożliwia:<br>
Rejestrację i logowanie użytkowników z wykorzystaniem JWT (HMAC SHA-256).<br>
Zarządzanie rakietami (tworzenie, odczyt, aktualizacja, usuwanie).<br>
Zarządzanie misjami (CRUD + paginacja, filtrowanie, sortowanie).<br>
Import danych z pliku CSV z informacjami o misjach i automatyczne tworzenie rekordów rakiet.<br>
End-pointy dla powiązanych zasobów, np. pobieranie rakiety powiązanej z daną misją.<br>
Swagger UI do dokumentacji i testowania API.<br>


### Opis metody zaimportowania danych z plików .csv
Import realizowany jest w kontrolerze CsvImportController:<br>
Odczyt pliku Data/space_missions.csv z katalogu głównego aplikacji.<br>
Parsowanie rekordów przy pomocy biblioteki CsvHelper (ustawienia MissingFieldFound=null, HeaderValidated=null).<br>


### Dla każdej unikalnej nazwy rakiety:<br>
Sprawdzenie czy istnieje w bazie (_context.Rockets).<br>
Utworzenie nowego obiektu Rocket jeśli nie istnieje.<br>
Parsowanie daty i czasu lotu (UTC) metodą TryParseDateTime.<br>
Konwersja ceny z formatu tekstowego (usunięcie znaków '$' i ',') przez TryParsePrice.<br>
Zgromadzenie wszystkich rekordów w obiektach Mission i zapis do bazy danych (razem z nowymi rakietami).<br>

### Opis uruchomienia aplikacji<br>
Konfiguracja:<br>
W pliku appsettings.json (lub appsettings.Development.json) podać łańcuch połączenia do PostgreSQL w sekcji ConnectionStrings:DefaultConnection.<br>
Ustawić klucz Jwt:Key, Jwt:Issuer oraz Jwt:Audience.<br>


### Budowa i uruchomienie:<br>
dotnet build <br>
dotnet run --project SpaceMissions.WebAP<br>

Domyślnie aplikacja dostępna jest pod https://localhost:5004 (HTTP).<br>

### API: <br>
Dokumentacja Swagger: https://localhost:5004/swagger 

* End-point rejestracji: POST /api/auth/register <br>
* End-point logowania: POST /api/auth/login <br>

End-pointy CRUD:<br>
* /api/rockets<br>
* /api/missions <br>
* /api/missions/import (import CSV) <br>

### Testy automatyczne: <br>

Uruchomić dotnet test w katalogu SpaceMissions.WebAP.Tests dla testów kontrolerów.<br>