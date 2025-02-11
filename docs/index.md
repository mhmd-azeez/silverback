---
documentType: index
title: "Home"
---

<div role="main" class="hide-when-search">
    <div style="background-color: #000;">
        <div class="container body-container">
            <div class="hero" style="background-image: url('images/splash.jpg');">
                <div class="wrapper">
                    <h1 id="page-title" class="page-title" itemprop="headline">        
                        Silverback
                    </h1>
                    <p class="lead">
                        A simple but feature-rich framework to build reactive/event-driven applications with .net core
                    </p>
                    <p>
                        <a href="https://github.com/BEagle1984/silverback/" class="btn"><i class="fab fa-github"></i> View on GitHub</a>
                        <a href="https://www.nuget.org/packages?q=Silverback" class="btn"><i class="fas fa-arrow-alt-circle-down"></i> Get from NuGet</a>
                    </p>
                </div>
            </div>
        </div>
    </div>
    <div class="container body-container body-content keys">
        <div class="row">
            <div class="col-md-4 key">
                <i class="icon fas fa-paper-plane"></i>
                <h2>Simple yet powerful message bus</h2>
                <p>
                    The in-memory message bus (the mediator) is very simple to use but it is very flexible and can be used in a multitude of use cases.
                </p>
                <p>
                    Silverback also ships with native support for Rx.net (System.Reactive).
                </p>
            </div>
            <div class="col-md-4 key">
                <i class="icon fas fa-plug"></i>
                <h2>Message broker abstraction</h2>
                <p>
                    The bus can be connected with a message broker to integrate different services or application. The integration happens configuratively at startup and it is then completely abstracted.
                </p>
                <p>
                    Silverback corrently provides a package to connect with the very popular <a href="https://kafka.apache.org/">Apache Kafka</a> message broker, <a href="https://www.rabbitmq.com/">RabbitMQ</a> or any <a href="https://mqtt.org/">MQTT</a> broker.
                </p>
                <p>
                    Integrating other message brokers wouldn't be a big deal and some will be added in the future...or feel free to create your own <code>IBroker</code> implementation.
                </p>
            </div>
            <div class="col-md-4 key">
                <i class="icon fas fa-atom"></i>
                <h2>Transactional messaging</h2>
                <p>
                    One of the main challenges related to asynchronous messaging is maintaining consistency, for example updating the database and sending the messages as part of an atomic transaction. Silverback solves this problem with the built-in implementation of the outbox table pattern, where the outbound messages are temporary inserted in a database table inside the regular transaction.
                </p>
                <p>
                    Silverback integrates seemlessly with EntityFramework Core (but could be extended to plug-in other ORMs).
                </p>
            </div>
        </div>
        <div class="row">
            <div class="col-md-4 key">
                <i class="icon fas fa-pencil-ruler"></i>
                <h2>Domain Driven Design</h2>
                <p>
                    Built-in support for enhanced domain entities that create domain events to be automatically published whenever the entity is persisted to the database.
                </p>
                <p>
                    When properly configured, these events can be automatically be forwarded to the message broker of choice.
                </p>
            </div>
            <div class="col-md-4 key">
                <i class="icon fas fa-thumbs-up"></i>
                <h2>Exactly-once processing</h2>
                <p>
                    Silverback can automatically keep track of the messages that have been consumed in order to guarantee that each message is processed exactly once.
                </p>
                <p>
                    This information can be stored in the database to be again transactional with the changes made to the local data, ensuring that the changes are applied once and only once to the data.
                </p>
            </div>
            <div class="col-md-4 key">
                <i class="icon fas fa-exclamation-triangle"></i>
                <h2>Error handling policies</h2>
                <p>
                    Sooner or later you will run into an issue with a message that cannot be processed and you therefore have to handle the exception and decide what to do with the message.
With Silverback you can configure some error handling policies for each inbound endpoint. The built-in policies are:
                    <ul>
                        <li><b>Skip</b>: simply ignore the message</li>
                        <li><b>Retry</b>: retry the same message (delays can be specified)</li>
                        <li><b>Move</b>: move the message to another topic/queue (or re-enqueue it at the end of the same one)</li>
                    </ul>
                </p>
                <p>
                    Combining this three policies you will be able to implement pretty much any use case.
                </p>
            </div>
        </div>
        <div class="row">
            <div class="col-md-4 key">
                <i class="icon fas fa-shoe-prints"></i>
                <h2>Distributed tracing</h2>
                <p>
                    Silverback integrates with <code>System.Diagnostics</code> to ensure the entire flow can easily be traced, also when involving a message broker.
                </p>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12 more">
                <a href="xref:introduction" class="btn btn-default btn-lg"><i class="fas fa-search-plus"></i> Discover more</a>
            </div>
        </div>
    </div>
</div>
