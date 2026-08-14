# Walkthrough: Saga Transaction & Rollback Pattern Demo

I have implemented a simulated **Saga Pattern** transaction workflow showing forward steps and rollback (compensating transactions) using Prism EventAggregator in our WPF project.

## Changes Completed

### 1. Created Saga-Specific Event Payloads & Events
Created the following event classes under the `Events` directory:
- [OrderCreatedEvent.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Events/OrderCreatedEvent.cs)
- [InventoryReservedEvent.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Events/InventoryReservedEvent.cs)
- [PaymentProcessedEvent.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Events/PaymentProcessedEvent.cs)
- [PaymentFailedEvent.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Events/PaymentFailedEvent.cs)
- [OrderFailedEvent.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Events/OrderFailedEvent.cs)
- [TransactionLogEvent.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Events/TransactionLogEvent.cs) (For console logging)

### 2. Created Transactional Services
- [InventoryService.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Services/InventoryService.cs): Handles the forward transaction (reserving stock) and compensating transaction (releasing stock upon payment failure).
- [PaymentService.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Services/PaymentService.cs): Simulates billing and triggers `PaymentFailedEvent` when simulated failure is enabled.

### 3. Integrated Services & Registered in DI
- Registered both services as singletons in [App.xaml.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/App.xaml.cs) and resolved them on startup to bootstrap subscriptions.

### 4. Polished UI & Logging Mechanism
- Updated [OrderViewModel.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/ViewModels/OrderViewModel.cs) to bind logs and expose a simulation switch.
- Refactored [MainWindow.xaml](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/MainWindow.xaml) into a beautiful 2-column console/dashboard log panel.

## Build Results
The project compiled successfully with **0 warnings and 0 errors**.
