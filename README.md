# SFR_Messaging
This is a university assignment to practice the use of Apache Kafka.

## Usage
Type  
***docker-compose up***  
in the SFR_Messaging/SFR_Messaging directory which will first start zookeeper, then Kafka and lastly the .NET Core App.  

All information about the progress of the app is printed on the command line. Information regarding the .NET Core app starts with ***sfr_messaging***. In the docker-compose.yml logging output for zookeeper and kafka
are deactivated. If something goes wrong just comment out *logging: driver: none* and see if there is some message that is helping.

Every 60 Seconds the CoreBankingSystem sends a new payment. This payment will be consumed by the MoneyLaunderingService, which will then check if the payment should be declined due to money laundering (implemented as fuzzy logic - payments larger than EUR 1.000).
The MoneyLaunderingService will then write another event, which will be consumed by the TransactionAnalyticsService. This service then simply checks if the payment was accepted or declined and prints the statistics of declined/accepted payments.
