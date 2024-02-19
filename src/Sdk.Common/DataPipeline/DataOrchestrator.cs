namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

// public class DataOrchestrator
// {
//     public void ProcessData(object source)
//     {
//         
//         
//         var dto = dataService.RetrieveData();
//         if (dto != null)
//         {
//             dtoBuffer.AddToBuffer(dto);
//             publisher.Publish(dto);
//             dtoBuffer.RemoveFromBuffer(dto); // Ideally, this should be based on publish success callback
//         }
//     }
// }