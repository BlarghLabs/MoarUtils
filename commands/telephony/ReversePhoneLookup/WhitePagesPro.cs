using System;
using System.Net;
using MoarUtils.commands.logging;
using Newtonsoft.Json;
using RestSharp;

namespace MoarUtils.Utils.Telephony.ReversePhoneLookup {
  public class WhitePagesPro {
    public const string BASE = "https://proapi.whitepages.com/";

    public static bool ExecuteSync(
      string phoneNumber,
      string apiKey,
      out string name,
      out string address
    ) {
      name = null;
      address = null;

      try {
        var resource = "3.0/phone?phone=" + phoneNumber + "&api_key=" + apiKey;
        var client = new RestClient(BASE);
        var request = new RestRequest {
          Resource = resource,
          Method = Method.Get,
          RequestFormat = DataFormat.Json,
        };

        var response = client.ExecuteAsync(request).Result;
        var content = response.Content;
        if (response.ErrorException != null) {
          throw response.ErrorException;
        }
        if (response.StatusCode != HttpStatusCode.OK) {
          LogIt.W(response.StatusCode);
        }

        dynamic json = JsonConvert.DeserializeObject(content);
        if (json.is_valid.Value == true) {
          //"type": "Person",  
          name = json.belongs_to[0].name.Value;
        }
        //TODO: parse name and address

        return response.StatusCode.Equals(HttpStatusCode.OK);
      } catch (Exception ex) {
        LogIt.E(ex);
      } finally {
      }
      return false;
    }
  }
}

//{
//  "id": "Phone.c31f6fef-a2e1-4b08-cfe3-bc7128b728f2.Durable",
//  "phone_number": "XXXXXXXX",
//  "is_valid": true,
//  "country_calling_code": "1",
//  "line_type": "Mobile",
//  "carrier": "AT&T",
//  "is_prepaid": null,
//  "is_commercial": false,
//  "belongs_to": [
//    {
//      "id": "Person.3a33088c-436f-4635-8fee-9e01beaedd29.Durable",
//      "name": "FIRST LAST",
//      "firstname": "FIRST",
//      "middlename": null,
//      "lastname": "LAST",
//      "age_range": null,
//      "gender": null,
//      "type": "Person",
//      "link_to_phone_start_date": null
//    }
//  ],
//  "current_addresses": [
//    {
//      "id": "Location.c8e9c102-76f5-4eb6-ad15-21e4db0fd0d6.Durable",
//      "location_type": "ZipPlus4",
//      "street_line_1": null,
//      "street_line_2": null,
//      "city": "Washington",
//      "postal_code": "20009",
//      "zip4": "2551",
//      "state_code": "DC",
//      "country_code": "US",
//      "lat_long": {
//        "latitude": 38.912023,
//        "longitude": -77.041835,
//        "accuracy": "Street"
//      },
//      "is_active": null,
//      "delivery_point": null,
//      "link_to_person_start_date": null
//    }
//  ],
//  "historical_addresses": [

//  ],
//  "associated_people": [

//  ],
//  "alternate_phones": [

//  ],
//  "error": null,
//  "warnings": [

//  ]
//}