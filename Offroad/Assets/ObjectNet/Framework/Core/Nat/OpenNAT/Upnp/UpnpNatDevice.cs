
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace com.onlineobject.objectnet {
	/// <summary>
	/// Represents a UPnP NAT device and provides functionality to interact with it.
	/// </summary>
	internal sealed class UpnpNatDevice : NatDevice {
        /// <summary>
        /// Gets the host endpoint of the UPnP NAT device.
        /// </summary>
        public override IPEndPoint HostEndPoint {
            get { return DeviceInfo.HostEndPoint; }
        }

        /// <summary>
        /// Gets the local IP address of the UPnP NAT device.
        /// </summary>
        public override IPAddress LocalAddress {
            get { return DeviceInfo.LocalAddress; }
        }

        // Stores the device information for the UPnP NAT device.
        internal readonly UpnpNatDeviceInfo DeviceInfo;

        // The SOAP client used to communicate with the UPnP NAT device.
        private readonly SoapClient _soapClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpnpNatDevice"/> class.
        /// </summary>
        /// <param name="deviceInfo">The device information for the UPnP NAT device.</param>
        internal UpnpNatDevice(UpnpNatDeviceInfo deviceInfo) {
            Touch();
            DeviceInfo = deviceInfo;
            _soapClient = new SoapClient(DeviceInfo.ServiceControlUri, DeviceInfo.ServiceType);
        }

#if NET35
    /// <summary>
    /// Asynchronously retrieves the external IP address of the UPnP NAT device.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the external IP address.</returns>
    public override Task<IPAddress> GetExternalIPAsync()
    {
        NatDiscoverer.TraceSource.LogInfo("GetExternalIPAsync - Getting external IP address");
        var message = new GetExternalIPAddressRequestMessage();
        return _soapClient
            .InvokeAsync("GetExternalIPAddress", message.ToXml())
            .TimeoutAfter(TimeSpan.FromSeconds(4))
            .ContinueWith(task =>
            {
                var responseData = task.Result;
                var response = new GetExternalIPAddressResponseMessage(responseData, DeviceInfo.ServiceType);
                return response.ExternalIPAddress;
            });
    }
#else
        /// <summary>
        /// Asynchronously retrieves the external IP address of the UPnP NAT device.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the external IP address.</returns>
        public override async Task<IPAddress> GetExternalIPAsync() {
            NatDiscoverer.TraceSource.LogInfo("GetExternalIPAsync - Getting external IP address");
            var message = new GetExternalIPAddressRequestMessage();
            var responseData = await _soapClient
                .InvokeAsync("GetExternalIPAddress", message.ToXml())
                .TimeoutAfter(TimeSpan.FromSeconds(4));

            var response = new GetExternalIPAddressResponseMessage(responseData, DeviceInfo.ServiceType);
            return response.ExternalIPAddress;
        }
#endif

#if NET35
		/// <summary>
		/// Asynchronously creates a port mapping on the UPnP NAT device.
		/// </summary>
		/// <param name="mapping">The mapping to create.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public override Task CreatePortMapAsync(Mapping mapping)
		{
			Guard.IsNotNull(mapping, "mapping");
			if (mapping.PrivateIP.Equals(IPAddress.None)) mapping.PrivateIP = DeviceInfo.LocalAddress;

			NatDiscoverer.TraceSource.LogInfo("CreatePortMapAsync - Creating port mapping {0}", mapping);

			var message = new CreatePortMappingRequestMessage(mapping);
			return _soapClient
				.InvokeAsync("AddPortMapping", message.ToXml())
				.TimeoutAfter(TimeSpan.FromSeconds(4))
				.ContinueWith(task =>
				{
					if (!task.IsFaulted)
					{
						RegisterMapping(mapping);
					}
					else
					{
						MappingException me = task.Exception.InnerException as MappingException;

						if (me == null)
						{
							throw task.Exception.InnerException;
						}

						switch (me.ErrorCode)
						{
							case UpnpConstants.OnlyPermanentLeasesSupported:
								NatDiscoverer.TraceSource.LogWarn(
									"Only Permanent Leases Supported - There is no warranty it will be closed");
								mapping.Lifetime = 0;
								// We create the mapping anyway. It must be released on shutdown.
								mapping.LifetimeType = MappingLifetime.ForcedSession;
								CreatePortMapAsync(mapping);
								break;
							case UpnpConstants.SamePortValuesRequired:
								NatDiscoverer.TraceSource.LogWarn(
									"Same Port Values Required - Using internal port {0}", mapping.PrivatePort);
								mapping.PublicPort = mapping.PrivatePort;
								CreatePortMapAsync(mapping);
								break;
							case UpnpConstants.RemoteHostOnlySupportsWildcard:
								NatDiscoverer.TraceSource.LogWarn("Remote Host Only Supports Wildcard");
								mapping.PublicIP = IPAddress.None;
								CreatePortMapAsync(mapping);
								break;
							case UpnpConstants.ExternalPortOnlySupportsWildcard:
								NatDiscoverer.TraceSource.LogWarn("External Port Only Supports Wildcard");
								throw me;
							case UpnpConstants.ConflictInMappingEntry:
								NatDiscoverer.TraceSource.LogWarn("Conflict with an already existing mapping");
								throw me;

							default:
								throw me;
						}
					}
				});
		}
#else
        /// <summary>
        /// Asynchronously creates a port mapping on the UPnP NAT device.
        /// </summary>
        /// <param name="mapping">The mapping to create.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task CreatePortMapAsync(Mapping mapping)
		{
			Guard.IsNotNull(mapping, "mapping");
			if(mapping.PrivateIP.Equals(IPAddress.None)) mapping.PrivateIP =  DeviceInfo.LocalAddress;

			NatDiscoverer.TraceSource.LogInfo("CreatePortMapAsync - Creating port mapping {0}", mapping);
			bool retry = false;
			try
			{
				var message = new CreatePortMappingRequestMessage(mapping);
				await _soapClient
					.InvokeAsync("AddPortMapping", message.ToXml())
					.TimeoutAfter(TimeSpan.FromSeconds(4));
				RegisterMapping(mapping);
			}
			catch(MappingException me)
			{
				switch (me.ErrorCode)
				{
					case UpnpConstants.OnlyPermanentLeasesSupported:
						NatDiscoverer.TraceSource.LogWarn("Only Permanent Leases Supported - There is no warranty it will be closed");
						mapping.Lifetime = 0;
						// We create the mapping anyway. It must be released on shutdown.
						mapping.LifetimeType = MappingLifetime.ForcedSession;
						retry = true;
						break;
					case UpnpConstants.SamePortValuesRequired:
						NatDiscoverer.TraceSource.LogWarn("Same Port Values Required - Using internal port {0}", mapping.PrivatePort);
						mapping.PublicPort = mapping.PrivatePort;
						retry = true;
						break;
					case UpnpConstants.RemoteHostOnlySupportsWildcard:
						NatDiscoverer.TraceSource.LogWarn("Remote Host Only Supports Wildcard");
						mapping.PublicIP = IPAddress.None;
						retry = true;
						break;
					case UpnpConstants.ExternalPortOnlySupportsWildcard:
						NatDiscoverer.TraceSource.LogWarn("External Port Only Supports Wildcard");
						throw;
					case UpnpConstants.ConflictInMappingEntry:
						NatDiscoverer.TraceSource.LogWarn("Conflict with an already existing mapping");
						throw;

					default:
						throw;
				}
			}
			if (retry)
				await CreatePortMapAsync(mapping);
		}
#endif

#if NET35
		/// <summary>
		/// Asynchronously deletes a port mapping from the UPnP NAT device.
		/// </summary>
		/// <param name="mapping">The mapping to delete.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public override Task DeletePortMapAsync(Mapping mapping)
		{
			Guard.IsNotNull(mapping, "mapping");

			if (mapping.PrivateIP.Equals(IPAddress.None)) mapping.PrivateIP = DeviceInfo.LocalAddress;

			NatDiscoverer.TraceSource.LogInfo("DeletePortMapAsync - Deleteing port mapping {0}", mapping);

			var message = new DeletePortMappingRequestMessage(mapping);
			return _soapClient
				.InvokeAsync("DeletePortMapping", message.ToXml())
				.TimeoutAfter(TimeSpan.FromSeconds(4))
				.ContinueWith(task =>
				{
					if (!task.IsFaulted)
					{
						UnregisterMapping(mapping);
					}
					else
					{
						MappingException e = task.Exception.InnerException as MappingException;
						if (e != null && e.ErrorCode != UpnpConstants.NoSuchEntryInArray) throw e;
					}
				});
		}
#else
        /// <summary>
        /// Asynchronously deletes a port mapping from the UPnP NAT device.
        /// </summary>
        /// <param name="mapping">The mapping to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task DeletePortMapAsync(Mapping mapping)
		{
			Guard.IsNotNull(mapping, "mapping");

			if (mapping.PrivateIP.Equals(IPAddress.None)) mapping.PrivateIP = DeviceInfo.LocalAddress;
			
			NatDiscoverer.TraceSource.LogInfo("DeletePortMapAsync - Deleteing port mapping {0}", mapping);

			try
			{
				var message = new DeletePortMappingRequestMessage(mapping);
				await _soapClient
					.InvokeAsync("DeletePortMapping", message.ToXml())
					.TimeoutAfter(TimeSpan.FromSeconds(4));
				UnregisterMapping(mapping);
			}
			catch (MappingException e)
			{
				if(e.ErrorCode != UpnpConstants.NoSuchEntryInArray) throw; 
			}
		}
#endif

#if NET35
		 /// <summary>
		/// Asynchronously retrieves a generic port mapping entry from the UPnP device and adds it to the provided list.
		/// </summary>
		/// <param name="index">The index of the port mapping entry to retrieve.</param>
		/// <param name="mappings">The list to which the retrieved mapping will be added.</param>
		/// <param name="taskCompletionSource">The TaskCompletionSource to signal completion.</param>
		public void GetGenericMappingAsync(int index, List<Mapping> mappings,
			TaskCompletionSource<IEnumerable<Mapping>> taskCompletionSource)
		{
			var message = new GetGenericPortMappingEntry(index);

			_soapClient
				.InvokeAsync("GetGenericPortMappingEntry", message.ToXml())
				.TimeoutAfter(TimeSpan.FromSeconds(4))
				.ContinueWith(task =>
				{
					if (!task.IsFaulted)
					{
						var responseData = task.Result;
						var responseMessage = new GetPortMappingEntryResponseMessage(responseData,
							DeviceInfo.ServiceType, true);

						IPAddress internalClientIp;
						if (!IPAddress.TryParse(responseMessage.InternalClient, out internalClientIp))
						{
							NatDiscoverer.TraceSource.LogWarn("InternalClient is not an IP address. Mapping ignored!");
						}
						else
						{
							var mapping = new Mapping(responseMessage.Protocol
								, internalClientIp
								, responseMessage.InternalPort
								, responseMessage.ExternalPort
								, responseMessage.LeaseDuration
								, responseMessage.PortMappingDescription);
							mappings.Add(mapping);
						}

						GetGenericMappingAsync(index + 1, mappings, taskCompletionSource);
					}
					else
					{
						MappingException e = task.Exception.InnerException as MappingException;

						if (e == null)
						{
							throw task.Exception.InnerException;
						}

						if (e.ErrorCode == UpnpConstants.SpecifiedArrayIndexInvalid
							|| e.ErrorCode == UpnpConstants.NoSuchEntryInArray)
						{
							// there are no more mappings
							taskCompletionSource.SetResult(mappings);
							return;
						}

						// DD-WRT Linux base router (and others probably) fails with 402-InvalidArgument when index is out of range
						if (e.ErrorCode == UpnpConstants.InvalidArguments)
						{
							NatDiscoverer.TraceSource.LogWarn("Router failed with 402-InvalidArgument. No more mappings is assumed.");
							taskCompletionSource.SetResult(mappings);
							return;
						}

						throw task.Exception.InnerException;
					}
				});
		}

		/// <summary>
	    /// Asynchronously retrieves all port mappings from the UPnP device.
		/// </summary>
		/// <returns>A task that represents the asynchronous operation and contains the list of mappings.</returns>
		public override Task<IEnumerable<Mapping>> GetAllMappingsAsync()
		{
			var taskCompletionSource = new TaskCompletionSource<IEnumerable<Mapping>>();

			NatDiscoverer.TraceSource.LogInfo("GetAllMappingsAsync - Getting all mappings");

			GetGenericMappingAsync(0, new List<Mapping>(), taskCompletionSource);
			return taskCompletionSource.Task;
		}
#else
        /// <summary>
        /// Asynchronously retrieves a specific port mapping entry from the NAT (Network Address Translation) device.
        /// </summary>
        /// <param name="protocol">The protocol (TCP/UDP) for which the mapping is to be retrieved.</param>
        /// <param name="publicPort">The public port number of the mapping to retrieve.</param>
        /// <returns>A <see cref="Mapping"/> object representing the port mapping entry if found; otherwise, null.</returns>
        /// <exception cref="MappingException">Thrown when an error occurs while retrieving the mapping.</exception>
        public override async Task<IEnumerable<Mapping>> GetAllMappingsAsync()
		{
			var index = 0;
			var mappings = new List<Mapping>();

			NatDiscoverer.TraceSource.LogInfo("GetAllMappingsAsync - Getting all mappings");
			while (true)
			{
				try
				{
					var message = new GetGenericPortMappingEntry(index++);

					var responseData = await _soapClient
						.InvokeAsync("GetGenericPortMappingEntry", message.ToXml())
						.TimeoutAfter(TimeSpan.FromSeconds(4));

					var responseMessage = new GetPortMappingEntryResponseMessage(responseData, DeviceInfo.ServiceType, true);

					IPAddress internalClientIp;
					if(!IPAddress.TryParse(responseMessage.InternalClient, out internalClientIp))
					{
						NatDiscoverer.TraceSource.LogWarn("InternalClient is not an IP address. Mapping ignored!");
						continue;
					}

					var mapping = new Mapping(responseMessage.Protocol
						, internalClientIp
						, responseMessage.InternalPort
						, responseMessage.ExternalPort
						, responseMessage.LeaseDuration
						, responseMessage.PortMappingDescription);
					mappings.Add(mapping);
				}
				catch (MappingException e)
				{
					// there are no more mappings
					if (e.ErrorCode == UpnpConstants.SpecifiedArrayIndexInvalid
					 || e.ErrorCode == UpnpConstants.NoSuchEntryInArray
					 // DD-WRT Linux base router (and others probably) fails with 402-InvalidArgument when index is out of range
					 || e.ErrorCode == UpnpConstants.InvalidArguments
					 // LINKSYS WRT1900AC AC1900 it returns errocode 501-PAL_UPNP_SOAP_E_ACTION_FAILED
					 || e.ErrorCode == UpnpConstants.ActionFailed)
					{
						NatDiscoverer.TraceSource.LogWarn("Router failed with {0}-{1}. No more mappings is assumed.", e.ErrorCode, e.ErrorText);
						break; 
					}
					throw;
				}
			}

			return mappings.ToArray();
		}
#endif

#if NET35
		public override Task<Mapping> GetSpecificMappingAsync(Protocol protocol, int publicPort)
		{
			Guard.IsTrue(protocol == Protocol.Tcp || protocol == Protocol.Udp, "protocol");
			Guard.IsInRange(publicPort, 0, ushort.MaxValue, "port");

			NatDiscoverer.TraceSource.LogInfo("GetSpecificMappingAsync - Getting mapping for protocol: {0} port: {1}", Enum.GetName(typeof(Protocol), protocol), publicPort);

			var message = new GetSpecificPortMappingEntryRequestMessage(protocol, publicPort);
			return _soapClient
				.InvokeAsync("GetSpecificPortMappingEntry", message.ToXml())
				.TimeoutAfter(TimeSpan.FromSeconds(4))
				.ContinueWith(task =>
				{
					if (!task.IsFaulted)
					{
						var responseData = task.Result;

						var messageResponse = new GetPortMappingEntryResponseMessage(responseData, DeviceInfo.ServiceType, false);

						return new Mapping(messageResponse.Protocol
							, IPAddress.Parse(messageResponse.InternalClient)
							, messageResponse.InternalPort
							, publicPort // messageResponse.ExternalPort is short.MaxValue
							, messageResponse.LeaseDuration
							, messageResponse.PortMappingDescription);
					}
					else
					{
						MappingException e = task.Exception.InnerException as MappingException;
						if (e != null && e.ErrorCode == UpnpConstants.NoSuchEntryInArray) return null;

						// DD-WRT Linux base router (and others probably) fails with 402-InvalidArgument 
						// when no mapping is found in the mappings table
						if (e != null && e.ErrorCode == UpnpConstants.InvalidArguments)
						{
							NatDiscoverer.TraceSource.LogWarn("Router failed with 402-InvalidArgument. Mapping not found is assumed.");
							return null;
						}
						throw task.Exception.InnerException;
					}
				});
		}
#else
		public override async Task<Mapping> GetSpecificMappingAsync (Protocol protocol, int publicPort)
		{
			Guard.IsTrue(protocol == Protocol.Tcp || protocol == Protocol.Udp, "protocol");
			Guard.IsInRange(publicPort, 0, ushort.MaxValue, "port");

			NatDiscoverer.TraceSource.LogInfo("GetSpecificMappingAsync - Getting mapping for protocol: {0} port: {1}", Enum.GetName(typeof(Protocol), protocol), publicPort);

			try
			{
				var message = new GetSpecificPortMappingEntryRequestMessage(protocol, publicPort);
				var responseData = await _soapClient
					.InvokeAsync("GetSpecificPortMappingEntry", message.ToXml())
					.TimeoutAfter(TimeSpan.FromSeconds(4));

				var messageResponse = new GetPortMappingEntryResponseMessage(responseData, DeviceInfo.ServiceType, false);

				if (messageResponse.Protocol != protocol)
					NatDiscoverer.TraceSource.LogWarn("Router responded to a protocol {0} query with a protocol {1} answer, work around applied.", protocol, messageResponse.Protocol);

				return new Mapping(protocol
					, IPAddress.Parse(messageResponse.InternalClient)
					, messageResponse.InternalPort
					, publicPort // messageResponse.ExternalPort is short.MaxValue
					, messageResponse.LeaseDuration
					, messageResponse.PortMappingDescription);
			}
			catch (MappingException e)
			{
				// there are no more mappings
				if (e.ErrorCode == UpnpConstants.SpecifiedArrayIndexInvalid
				 || e.ErrorCode == UpnpConstants.NoSuchEntryInArray
					// DD-WRT Linux base router (and others probably) fails with 402-InvalidArgument when index is out of range
				 || e.ErrorCode == UpnpConstants.InvalidArguments
					// LINKSYS WRT1900AC AC1900 it returns errocode 501-PAL_UPNP_SOAP_E_ACTION_FAILED
				 || e.ErrorCode == UpnpConstants.ActionFailed)
				{
					NatDiscoverer.TraceSource.LogWarn("Router failed with {0}-{1}. No more mappings is assumed.", e.ErrorCode, e.ErrorText);
					return null;
				}
				throw;
			}
		}
#endif
        /// <summary>
        /// Returns a string representation of the UPnP NAT device, including endpoint, control URL, service type, and last seen timestamp.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
		{
			//GetExternalIP is blocking and can throw exceptions, can't use it here.
			return String.Format(
				"EndPoint: {0}\nControl Url: {1}\nService Type: {2}\nLast Seen: {3}",
				DeviceInfo.HostEndPoint, DeviceInfo.ServiceControlUri, DeviceInfo.ServiceType, LastSeen);
		}
	}
}