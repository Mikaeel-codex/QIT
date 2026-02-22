# QIT — Quick Inventory Management System

QIT (Quick Inventory Management) is a desktop Point-of-Sale (POS) and inventory management system built using C#, WPF, and .NET.

The system is designed for small to medium-sized retail environments where transaction speed, stock accuracy, and role-based access control are critical.

This project demonstrates applied software engineering principles including MVVM architecture, data persistence using Entity Framework Core, and structured UI state management.

# Overview

QIT provides a fully functional retail workflow:

- Secure authentication with role separation (Admin / Cashier)

- Real-time product search and filtering

- Cart management with dynamic totals

- Automatic stock deduction on completed sales

- Inventory management (Create, Update, Delete)

- Transaction logging and sales tracking

The system is structured for maintainability, scalability, and clean separation of concerns.

# Architecture

The application follows the Model–View–ViewModel (MVVM) pattern.

### Structure

- Models
Business entities such as Product, CartLine, and User.

- Views
WPF XAML UI components.

- ViewModels
Application logic, command handling, state management, and data binding.

- Data Layer
Entity Framework Core integration with SQLite or SQL Server Express.

### Design Principles

- Separation of concerns

- Minimal UI logic in code-behind

- Command-based interaction using ICommand

- ObservableCollection for reactive UI updates

- Centralized database context management

# Technology Stack

Language: C#

- Framework: .NET

- UI Framework: WPF

- Architecture Pattern: MVVM

- ORM: Entity Framework Core

- Database: SQLite / SQL Server Express

# Core Features

### Authentication

- Role-based access control

- Restricted feature visibility by role

- Session-based user tracking

### Point of Sale

- Real-time product search

- Add/remove items from cart

- Dynamic subtotal and total calculation

- Automatic stock deduction on sale completion

- Receipt window generation

### Inventory Management

- Add new products

- Edit existing products

- Delete products

- Low stock monitoring

- Data validation for integrity

### Sales Logging

- Persistent transaction records

- Stock movement tracking

- Structured data persistence

# Project Structure

QIT/

Data/
  AppDbContext and database configuration

Models/
  Domain entities

ViewModels/
  Application logic and commands

Views/
  WPF XAML UI windows

Helpers/
  ObservableObject, RelayCommand, utility classes

App.xaml
  Application entry point

# Installation

1. Clone the repository:

git clone https://github.com/Mikaeel-codex/QIT.git

2. Open the solution file in Visual Studio.

3. Restore NuGet packages.

4. Build the solution.

5. Run the application (F5).

# Database Configuration

The application uses Entity Framework Core.

- The database can be configured in AppDbContext.

- SQLite or SQL Server Express may be used.

- Database creation and migrations can be enabled depending on configuration.

# Engineering Focus

This project demonstrates:

- Applied desktop application architecture

- MVVM pattern implementation

- Entity Framework data modeling

- Structured command-based UI interaction

- Retail workflow modeling

- State management and data consistency handling

# Future Improvements

- Sales analytics dashboard

- Multi-branch stock management

- Report export (Excel / PDF)

- Cloud synchronization layer

- Unit testing implementation

- Background task optimization

# Project Status

Active development.

Core functionality is stable. Architectural refinements and feature expansion are ongoing.

# Author

### Mikaeel Pathan

Bachelor of Computer and Information Sciences (Application Development)

GitHub: https://github.com/Mikaeel-codex
