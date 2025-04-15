Big Picture
===========

Ocelot is aimed at people using .NET running a microservices (service-oriented) architecture (aka  SOA) that need a unified point of entry into their system.
However it will work with anything that speaks HTTP(S) and run on any platform that `ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/>`_ supports.

Ocelot consists of a series of ASP.NET Core `middlewares <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/>`_ arranged in a specific order.

Ocelot manipulates the ``HttpRequest`` object into a state specified by its configuration until it reaches a request builder middleware,
where it creates a ``HttpRequestMessage`` object which is used to make a request to a downstream service.
The middleware that makes the request is the last thing in the Ocelot pipeline. It does not call the next middleware.
The response from the downstream service is retrieved as the request goes back up the Ocelot pipeline.
There is a piece of middleware that maps the ``HttpResponseMessage`` onto the ``HttpResponse`` object, and that is returned to the client.
That is basically it with a bunch of other features!

Basic Implementation
--------------------

The following are configurations that you use when deploying Ocelot.

.. image:: ../images/OcelotBasic.jpg

Multiple Instances
------------------

.. image:: ../images/OcelotMultipleInstances.jpg

With Consul
-----------

.. image:: ../images/OcelotMultipleInstancesConsul.jpg

With Service Fabric
-------------------

.. image:: ../images/OcelotServiceFabric.jpg
