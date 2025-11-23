using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ecommerce.Shared.Wrapper
{
    public class ResponseType<T>
    {
         // Properties (for direct property access when needed)
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();
    
        // Factory methods (for clean creation)
        public static ResponseType<T> SuccessResult(T data, string message) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };
         public static ResponseType<T> SuccessResult(string message) => new()
        {
            Success = true,
            Message = message
        };
        
        public static ResponseType<T> Fail(string message, string? additionalMessage = null) => new()
        {
            Success = false,
            Message = message,
            Data = default! 
        };
        
        //contains many error
        public static ResponseType<T> Fail(List<string> errors,string message) => new()
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    
        // Instance methods (for fluent-style building)
        public ResponseType<T> WithMessage(string message)
        {
            Message = message;
            return this;
        }
        
        //can add many error
        public ResponseType<T> AddError(string error)
        {
            Errors.Add(error);
            return this;
        }
    }
}
