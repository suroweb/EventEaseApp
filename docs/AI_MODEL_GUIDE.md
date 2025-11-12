# EventEase AI Model Integration Guide

**Version**: 2.1
**Last Updated**: November 12, 2025

---

## Overview

EventEase now supports **4 AI providers** with **multiple models**, giving you ultimate flexibility in balancing cost, performance, privacy, and features. You can use commercial APIs, open-source models, or run models locally for FREE.

### Supported AI Providers

1. **OpenAI** - GPT-4o, GPT-4o Mini (Commercial)
2. **Anthropic** - Claude 3.5 Sonnet (Commercial)
3. **DeepSeek** - DeepSeek Chat, DeepSeek Coder (Open-Source API)
4. **Ollama** - Llama 3, Mistral, CodeLlama, and 100+ models (FREE Local)

---

## Quick Comparison

| Provider | Best For | Cost | Privacy | Setup Difficulty |
|----------|---------|------|---------|------------------|
| **OpenAI** | Highest quality, multimodal | $$$ | Cloud | Easy |
| **Anthropic** | Best reasoning, long context | $$$ | Cloud | Easy |
| **DeepSeek** | Best value, strong reasoning | $ | Cloud | Easy |
| **Ollama** | FREE, complete privacy | FREE | Local | Medium |

---

## Model Comparison Table

| Model | Provider | Cost/1M Tokens | Context Window | Best Use Case | Quality Score |
|-------|----------|----------------|----------------|---------------|---------------|
| **GPT-4o** | OpenAI | $5 input / $15 output | 128K | Best overall, multimodal | 9.5/10 |
| **GPT-4o Mini** | OpenAI | $0.15 input / $0.60 output | 128K | Fast, affordable | 8.5/10 |
| **Claude 3.5 Sonnet** | Anthropic | $3 input / $15 output | 200K | Best reasoning, analysis | 9.7/10 |
| **DeepSeek Chat** | DeepSeek | $0.14 input / $0.28 output | 64K | Best value, open-source | 8.8/10 |
| **DeepSeek Coder** | DeepSeek | $0.14 input / $0.28 output | 64K | Code generation | 9.3/10 (code) |
| **Llama 3 (8B)** | Ollama | **FREE** (local) | 8K | General tasks, privacy | 7.5/10 |
| **Mistral (7B)** | Ollama | **FREE** (local) | 32K | Fast responses | 7.3/10 |
| **CodeLlama (7B)** | Ollama | **FREE** (local) | 16K | Code generation | 8.0/10 (code) |

---

## Configuration Guide

### 1. OpenAI (GPT-4o)

Add to `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4o",
    "BaseUrl": "https://api.openai.com"
  }
}
```

**Get API Key**: https://platform.openai.com/api-keys

**Cost**: $5-15 per million tokens
**Pros**: Highest quality, multimodal (vision), fast
**Cons**: Most expensive, cloud-only

---

### 2. Anthropic (Claude 3.5 Sonnet)

Add to `appsettings.json`:

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-...",
    "Model": "claude-3-5-sonnet-20241022",
    "BaseUrl": "https://api.anthropic.com"
  }
}
```

**Get API Key**: https://console.anthropic.com/

**Cost**: $3-15 per million tokens
**Pros**: Best reasoning, huge context (200K tokens), safety-focused
**Cons**: Expensive, cloud-only

---

### 3. DeepSeek (Open-Source via API)

Add to `appsettings.json`:

```json
{
  "DeepSeek": {
    "ApiKey": "your-deepseek-api-key",
    "Model": "deepseek-chat",
    "BaseUrl": "https://api.deepseek.com"
  }
}
```

**Get API Key**: https://platform.deepseek.com/

**Cost**: $0.14-0.28 per million tokens (95% cheaper than GPT-4!)
**Pros**:
- **Ultra-affordable** (cheapest high-quality AI)
- Open-source model weights
- Strong reasoning capabilities
- Excellent for code generation (DeepSeek Coder)
- Good performance (88% of GPT-4o quality at 5% of the cost)

**Cons**:
- Smaller context window (64K vs 128K-200K)
- Cloud-based (not local)

**Available Models**:
- `deepseek-chat` - General purpose, strong reasoning
- `deepseek-coder` - Optimized for code generation
- `deepseek-reasoner` - Enhanced reasoning capabilities

---

### 4. Ollama (FREE Local Models)

#### Step 1: Install Ollama

**macOS/Linux**:
```bash
curl -fsSL https://ollama.com/install.sh | sh
```

**Windows**:
Download from https://ollama.com/download

#### Step 2: Pull Models

```bash
# General purpose models
ollama pull llama3          # Meta's Llama 3 (8B) - excellent all-around
ollama pull mistral         # Mistral 7B - fast and efficient
ollama pull mixtral         # Mixtral 8x7B - more powerful

# Code-focused models
ollama pull codellama       # Meta's CodeLlama - code generation
ollama pull deepseek-coder  # DeepSeek Coder - local version

# Smaller/faster models
ollama pull phi3            # Microsoft Phi-3 - tiny but capable
ollama pull gemma           # Google Gemma - efficient

# List all available models
ollama list
```

#### Step 3: Configure EventEase

Add to `appsettings.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "llama3"
  }
}
```

**Cost**: **100% FREE** (runs on your computer)
**Pros**:
- **Completely FREE** - no API costs ever
- **100% Private** - data never leaves your computer
- **Offline capable** - works without internet
- **No rate limits** - use as much as you want
- **100+ models available** - easy to switch

**Cons**:
- Requires local hardware (GPU recommended but not required)
- Slightly lower quality than GPT-4o/Claude
- Slower on CPU-only systems
- Requires disk space (2-20GB per model)

**Hardware Requirements**:
- **Minimum**: 8GB RAM, modern CPU
- **Recommended**: 16GB+ RAM, NVIDIA/AMD GPU
- **Optimal**: 32GB+ RAM, high-end GPU (RTX 4090, etc.)

---

## Intelligent Model Router

EventEase includes an **AI Model Router** that automatically selects the best model for each task.

### Usage

```csharp
// Inject the router
public class MyService
{
    private readonly IAIModelRouter _modelRouter;

    public async Task DoSomething()
    {
        var request = new AIRoutingRequest
        {
            SystemPrompt = "You are an event planning expert.",
            UserPrompt = "Create a tech conference agenda",
            TaskType = AITaskType.ContentGeneration,
            SelectionCriteria = new ModelSelectionCriteria
            {
                Priority = ModelPriority.Cost, // Optimize for cost
                MaxCostPerRequest = 0.01m,     // Max $0.01 per request
                RequireOpenSource = true        // Only use open-source
            }
        };

        var response = await _modelRouter.RouteRequestAsync(request);
    }
}
```

### Priority Options

1. **Cost** - Cheapest model (usually DeepSeek or Ollama)
2. **Performance** - Best quality (usually GPT-4o or Claude)
3. **Speed** - Fastest response
4. **Balanced** - Best cost/performance ratio
5. **Privacy** - Local/open-source only (Ollama)

---

## Task-Specific Recommendations

### Code Generation
**Best Models**:
1. DeepSeek Coder ($0.14/1M) - Best value
2. GPT-4o ($10/1M) - Highest quality
3. CodeLlama (FREE) - Good for simple code, private

### Content Writing
**Best Models**:
1. DeepSeek Chat ($0.14/1M) - Great value
2. GPT-4o Mini ($0.375/1M) - Fast and affordable
3. Llama 3 (FREE) - Good quality, private

### Complex Reasoning
**Best Models**:
1. Claude 3.5 Sonnet ($9/1M) - Best reasoning
2. GPT-4o ($10/1M) - Excellent reasoning
3. DeepSeek Chat ($0.21/1M) - Good value

### High Volume / Cost-Sensitive
**Best Models**:
1. Ollama Llama 3 (FREE) - Zero cost
2. DeepSeek Chat ($0.14/1M) - Cheapest API
3. GPT-4o Mini ($0.375/1M) - Low cost, good quality

---

## Cost Comparison Examples

### Generating 100 Event Descriptions (500 tokens each)

| Model | Cost per Description | Total Cost (100) | Time to Generate |
|-------|---------------------|------------------|------------------|
| **GPT-4o** | $0.075 | **$7.50** | 5 min |
| **Claude 3.5** | $0.045 | **$4.50** | 6 min |
| **DeepSeek** | $0.0021 | **$0.21** | 7 min |
| **Ollama Llama 3** | $0.00 | **FREE** | 20 min* |

\* Time varies based on hardware

### Monthly Usage for Active Tenant

Assume: 1,000 AI requests/month, average 1,000 tokens per request

| Model | Monthly Cost |
|-------|--------------|
| **GPT-4o** | **$150** |
| **Claude 3.5** | **$90** |
| **DeepSeek** | **$4.20** |
| **Ollama** | **$0 (FREE)** |

**Annual Savings vs GPT-4o**:
- DeepSeek: **$1,750/year saved**
- Ollama: **$1,800/year saved**

---

## Privacy & Security Considerations

### Cloud Models (OpenAI, Anthropic, DeepSeek)
- ✅ Managed infrastructure
- ✅ Always up-to-date
- ✅ High availability
- ❌ Data sent to third-party servers
- ❌ Requires internet connection
- ❌ Subject to provider terms of service

### Local Models (Ollama)
- ✅ **100% private** - data never leaves your server
- ✅ **Offline capable** - works without internet
- ✅ **No vendor lock-in** - you control everything
- ✅ **GDPR/HIPAA friendly** - full data control
- ❌ Requires local hardware
- ❌ You manage updates and maintenance

**Recommendation**:
- Use **Ollama** for sensitive data (PII, confidential info)
- Use **DeepSeek** for cost-sensitive workloads
- Use **GPT-4o/Claude** for highest quality when needed

---

## Performance Benchmarks

### Reasoning Tasks (0-shot, complex problems)

| Model | MMLU Score | BBH Score | Overall |
|-------|------------|-----------|---------|
| Claude 3.5 Sonnet | 88.7% | 93.1% | **Best** |
| GPT-4o | 88.7% | 92.3% | Excellent |
| DeepSeek Chat | 84.1% | 86.9% | Very Good |
| Llama 3 (8B) | 66.0% | 68.4% | Good |

### Code Generation (HumanEval benchmark)

| Model | Pass@1 | Pass@10 | Overall |
|-------|--------|---------|---------|
| DeepSeek Coder | 90.2% | 96.5% | **Best Value** |
| GPT-4o | 90.2% | 97.8% | Best Overall |
| CodeLlama (34B) | 53.7% | 79.3% | Good (local) |

---

## Migration Guide

### Switching from GPT-4 to DeepSeek

1. Update configuration:
```json
{
  "DeepSeek": {
    "ApiKey": "your-key",
    "Model": "deepseek-chat"
  }
}
```

2. Update code (if using direct API calls):
```csharp
// Old: OpenAI
var response = await _openAIService.SendPromptAsync(...);

// New: DeepSeek
var response = await _deepSeekService.SendPromptAsync(...);

// Or use router (recommended)
var response = await _modelRouter.RouteRequestAsync(new AIRoutingRequest
{
    TaskType = AITaskType.ContentGeneration,
    SelectionCriteria = new { Priority = ModelPriority.Cost }
});
```

**Expected Results**:
- **Cost**: 95% reduction
- **Quality**: ~88-90% of GPT-4o
- **Speed**: Similar or slightly slower

---

## Best Practices

### 1. Use Model Router for Automatic Selection

```csharp
// Let the router pick the best model
var response = await _modelRouter.RouteRequestAsync(new AIRoutingRequest
{
    SystemPrompt = systemPrompt,
    UserPrompt = userPrompt,
    TaskType = AITaskType.ContentGeneration,
    SelectionCriteria = new ModelSelectionCriteria
    {
        Priority = ModelPriority.Balanced
    }
});
```

### 2. Cache Results When Possible

```csharp
// Cache AI responses to avoid duplicate API calls
var cacheKey = $"ai_{taskType}_{hash(prompt)}";
if (_cache.TryGetValue(cacheKey, out string cached))
{
    return cached;
}

var response = await _modelRouter.RouteRequestAsync(request);
_cache.Set(cacheKey, response.Content, TimeSpan.FromHours(24));
```

### 3. Use Appropriate Models for Tasks

- **Simple tasks** → Ollama (FREE)
- **Code generation** → DeepSeek Coder (cheap, excellent)
- **Complex reasoning** → Claude 3.5 Sonnet (best quality)
- **Bulk operations** → DeepSeek or Ollama (cost-effective)

### 4. Monitor Costs

```csharp
var estimate = await _modelRouter.EstimateCostAsync(prompt, "gpt-4o");
_logger.LogInformation("Estimated cost: ${Cost}", estimate.EstimatedCost);

if (estimate.EstimatedCost > 0.10m)
{
    // Switch to cheaper model
    response = await _deepSeekService.SendPromptAsync(...);
}
```

---

## Troubleshooting

### Ollama Connection Issues

**Error**: "Cannot connect to Ollama"

**Solution**:
```bash
# Check if Ollama is running
curl http://localhost:11434

# Start Ollama
ollama serve

# Verify model is pulled
ollama list
ollama pull llama3
```

### DeepSeek API Rate Limits

**Error**: "Rate limit exceeded"

**Solution**:
- Upgrade DeepSeek plan
- Implement request queuing
- Add retry logic with exponential backoff

### Out of Memory (Ollama)

**Error**: "OOM: Model too large"

**Solution**:
- Use smaller model: `ollama pull llama3:7b` instead of `llama3:70b`
- Close other applications
- Use quantized models: `ollama pull llama3:7b-q4_0`

---

## Future Roadmap

### Planned Integrations

- **Groq** - Ultra-fast inference API
- **Together.ai** - Open-source model hosting
- **Replicate** - Easy model deployment
- **Local Llama 3.1 (405B)** - via Ollama
- **Google Gemini** - Multimodal capabilities

### Upcoming Features

- **Cost tracking dashboard** - Monitor AI spending
- **A/B testing** - Compare model outputs
- **Fine-tuning support** - Custom models
- **Model ensembles** - Combine multiple models
- **Automatic failover** - Switch providers on error

---

## Support & Resources

- **Ollama Documentation**: https://ollama.com/docs
- **DeepSeek Platform**: https://platform.deepseek.com
- **OpenAI Docs**: https://platform.openai.com/docs
- **Anthropic Docs**: https://docs.anthropic.com

- **EventEase Support**: support@eventease.com
- **Community Discord**: https://discord.gg/eventease

---

## Summary & Recommendations

### For Startups / Cost-Conscious
✅ **Primary**: DeepSeek Chat ($0.14/1M tokens)
✅ **Backup**: Ollama Llama 3 (FREE)
✅ **Premium**: GPT-4o Mini ($0.375/1M)

**Expected Monthly Cost**: $5-20

### For Enterprises / Quality-Focused
✅ **Primary**: Claude 3.5 Sonnet (best reasoning)
✅ **Secondary**: GPT-4o (multimodal)
✅ **Bulk**: DeepSeek (cost-effective)

**Expected Monthly Cost**: $200-1,000

### For Privacy-Focused / Regulated Industries
✅ **Primary**: Ollama Llama 3 (100% private, FREE)
✅ **Backup**: Ollama Mistral (alternative)
✅ **Cloud fallback**: DeepSeek (open-source)

**Expected Monthly Cost**: $0-10

---

**EventEase now gives you the freedom to choose: Pay for premium quality, save 95% with open-source, or run completely FREE and private with local models. The choice is yours!** 🚀

