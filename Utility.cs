using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloserDataPipeline
{
    public static class Utility
    {
        public static RepositoryClientBase GetClient()
        {
            // The WcfRepositoryClient takes a configation object
            // detailing how to connect to the Repository.
            var connectionInfo = new RepositoryConnectionInfo()
            {
                // TODO Replace this with the hostname of your Colectica Repository
                Url = "localhost",
                AuthenticationMethod = RepositoryAuthenticationMethod.Windows,
                TransportMethod = RepositoryTransportMethod.NetTcp,
            };

            // Create the client object, passing in the connection information.
            var client = new WcfRepositoryClient(connectionInfo);
            return client;
        }
    }
}
