#!/bin/bash
cp sonic_title_page_640x360.png test.png 
cp test.png ./bin/Debug/netcoreapp3.1/test.png 
dotnet run > out.html 
head -n 12 out.html
