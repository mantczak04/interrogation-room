# Serwer posiada sekrety i udostępnia prywatne widoki

Host jest jedynym właścicielem pełnego stanu Rundy, w tym ról, pełnego Alibi, ukrytych faktów i Sekretnych Celów, a każdy klient otrzymuje wyłącznie przefiltrowany widok przeznaczony dla swojego gracza. Sekrety nie są globalnymi `SyncVar` ani stanem rozsyłanym wszystkim klientom; wybieramy celowane wiadomości Mirror, aby przypadkowe ujawnienie danych nie wynikało z wygody synchronizacji UI.
