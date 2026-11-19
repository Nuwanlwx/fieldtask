namespace FieldTask.Api.Services;

// Gemini answered, but the answer was unusable (out-of-range fields, missing data).
// Mapped to 502 Bad Gateway: the upstream service sent a bad response, not the caller.
// Retrying may help - model output varies from call to call.
public class AiResponseInvalidException(string message) : Exception(message);

// Gemini could not answer at all (quota exhausted, outage, timeouts after retries).
// Mapped to 503 Service Unavailable: retrying immediately will not help.
public class AiUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
