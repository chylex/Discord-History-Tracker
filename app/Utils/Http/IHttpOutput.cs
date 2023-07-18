using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DHT.Utils.Http;

public interface IHttpOutput {
	Task WriteTo(HttpResponse response);
}
