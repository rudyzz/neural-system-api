# Neural System API

An API communicating, streaming, classifying and sending therapy signals with neural systems.

## Introduction

This is a course project provided by UW ECE master's program. 

As part of the project, an API that served as a bridge between the modern neural system and the application is proposed. The primary purpose of this API was to facilitate seamless communication and signals control with the neural system while ensuring its stability and preventing timeouts.

To achieve this, the API continuously sent watchdog reset signals to the neural system, preventing it from shutting down due to timeouts. Additionally, the API implemented a logging mechanism to track the signals sent by the neural system. These signals were both printed to the terminal for real-time monitoring and logged into a dedicated log file for future analysis and troubleshooting.

Furthermore, the API incorporated a machine learning technique utilizing Latent Dirichlet Allocation (LDA) to diagnose whether the current signals indicated a normal or abnormal brain state. This functionality enabled the system to detect abnormal brain states and initiate appropriate actions. For instance, the API controlled the therapy on and off signals to administer treatment to patients experiencing abnormal brain seizures.

By integrating this API into the project, we achieved a robust and efficient communication channel between the neural system and the application. The logging and diagnostic capabilities provided valuable insights for monitoring and analyzing the system's behavior. Moreover, the integration of machine learning techniques enabled timely intervention and appropriate therapy, improving patient care and outcomes.

## Features

* Communication on/off
* Watchdog reset
* Reconnection try
* Multithread signal logging
* Signals pattern learning with LDA
* Therapy signals control

## Getting Started

### Prerequisites

Running on Visual Studio based on .NET 6.0

Hardware simulated by Arduino

