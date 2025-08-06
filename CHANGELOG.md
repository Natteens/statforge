# StatForge Changelog

## [1.0.0] - 2024-12-06

### 🚀 MAJOR REFACTORING - Ultra-Simplified API

Esta versão representa uma refatoração completa do StatForge, introduzindo uma API ultra-simplificada que mantém compatibilidade total com o sistema anterior.

### ✨ Novidades

#### API Ultra-Simplificada
- **[Stat] Attribute**: Marque qualquer campo com `[Stat]` e ele automaticamente se torna um atributo gerenciado
- **Sintaxe Natural**: `Health -= Time.deltaTime;` funciona exatamente como variáveis normais
- **Zero Configuração**: Funciona imediatamente sem setup adicional

#### Arquitetura Moderna
- **IAttribute<T>**: Interface genérica type-safe para todos os atributos
- **AttributeCollection**: Sistema thread-safe de gerenciamento de atributos
- **StatForgeComponent**: Componente moderno que funciona com ou sem MonoBehaviour

#### Performance Enterprise
- **Event Bus**: Sistema de eventos performático com pooling para zero allocations
- **Smart Caching**: Cache inteligente com TTL configurável para cálculos frequentes
- **Lazy Loading**: Carregamento sob demanda para operações custosas
- **Batch Operations**: Operações em lote para múltiplas mudanças atômicas

#### Sistema de Validação
- **Validation Rules**: Sistema plugável de validação com regras customizáveis
- **Common Rules**: Regras pré-definidas para casos comuns (positivos, ranges, etc.)

#### Editor Visual Moderno
- **Interface Limpa**: Removidos elementos visuais desnecessários
- **Workflow Simplificado**: Navegação intuitiva e funcionalidades essenciais
- **Live Preview**: Visualização em tempo real de atributos descobertos
- **Welcome Screen**: Guia de início rápido integrado

#### Query System Fluente
```csharp
var combatPower = statForge.Query()
    .Where(name => name.Contains("Attack"))
    .Sum<int>();
```

### 🔄 Compatibilidade
- **100% Backward Compatible**: Todo código existente continua funcionando
- **Migration Path**: Sistema tradicional pode ser usado lado a lado com o novo
- **Deprecation Warnings**: Avisos para APIs que serão descontinuadas no futuro

### 📚 Exemplos
- **PlayerExample.cs**: Demonstra a API básica ultra-simplificada
- **AdvancedPlayerExample.cs**: Mostra recursos enterprise (validation, batching, events)

### 🛠️ Melhorias Técnicas
- Thread-safety em todas as operações críticas
- Pooling de objetos para reduzir garbage collection
- Cache com invalidação inteligente
- Event system com type safety
- Reflection otimizada para auto-discovery

---

## [0.2.2](https://github.com/Natteens/statforge/compare/v0.2.1...v0.2.2) (2025-07-28)


### Bug Fixes

* Refactor container editor and improve template creation ([620fcea](https://github.com/Natteens/statforge/commit/620fcea14c41a920b677eeddd2f7b85673f6d2f2))

## [0.2.1](https://github.com/Natteens/statforge/compare/v0.2.0...v0.2.1) (2025-07-28)


### Bug Fixes

* Refactor StatForgeManager UI and add settings panel ([e76d7ca](https://github.com/Natteens/statforge/commit/e76d7cab58f0474f9915b37cbade036053c38283))

# [0.2.0](https://github.com/Natteens/statforge/compare/v0.1.0...v0.2.0) (2025-07-28)


### Features

* Add StatForge attribute system and editor tools ([c323ae6](https://github.com/Natteens/statforge/commit/c323ae613b034a32c9f2dd72e49bde9bdfe01ded))

# 📝 Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Não Lançado]

## [0.1.0] - 2025-07-27

### Adicionado
- ✨ Estrutura inicial do pacote Unity
- 📦 Configuração do Package Manager
- 📚 Documentação básica
- 🧪 Estrutura de testes
- 📋 Exemplos e amostras

### Mudado
- Nada ainda

### Removido
- Nada ainda

### Corrigido
- Nada ainda

---

Os tipos de mudanças são:
- **Adicionado** para novas funcionalidades
- **Mudado** para mudanças em funcionalidades existentes
- **Depreciado** para funcionalidades que serão removidas em breve
- **Removido** para funcionalidades removidas
- **Corrigido** para correções de bugs
- **Segurança** para vulnerabilidades
