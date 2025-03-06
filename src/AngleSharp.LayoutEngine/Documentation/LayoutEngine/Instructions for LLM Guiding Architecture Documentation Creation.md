# Instructions for LLM: Guiding Architecture Documentation Creation

Follow these step-by-step instructions to assist users in creating comprehensive architectural documentation for the AngleSharp Layout Engine. This process is interactive, requiring confirmation or refinement at each step before proceeding.

## General Approach

1. Always follow an interactive, step-by-step approach
2. After completing each major section, ask if the user wants to continue to the next section or refine the current one
3. If refinement is requested, guide the user through improving that section
4. Provide examples and suggestions based on the available AngleSharp Layout Engine context
5. At the end of each document, summarize what has been created and suggest next steps

## Document Creation Process

### Step 1: Determine Document Type

Start by asking which type of document the user wants to create:

- Architecture Document
- Implementation Plan
- Implementation Status

Based on their selection, follow the appropriate process below.

## Architecture Document Creation Process

### Step 1: System Identification

1. Ask which system the architecture document is for:
    
    - StyleSystem
    - LayoutSystem
    - LifecycleSystem
    - CacheSystem
    - Another custom system
    - Overall AngleSharp Layout Engine architecture
2. Explain the purpose of the selected system based on your knowledge of AngleSharp Layout Engine.
    
3. Ask for confirmation or refinement of the system scope.
    

### Step 2: System Overview

1. Guide the user to create a System Overview section:
    
    ```
    ## 1. System Overview
    
    ### 1.1 Purpose and Scope
    [Brief description of the system's purpose, what problem it solves, and its scope]
    
    ### 1.2 Core Design Principles
    - **Principle 1**: [Description]
    - **Principle 2**: [Description]
    - **Principle 3**: [Description]
    
    ### 1.3 System Context
    [Describe how this system fits into the larger AngleSharp Layout Engine architecture]
    ```
    
2. Provide suggestions for content based on the selected system. For example, for StyleSystem:
    
    - Purpose: Computing CSS styles for DOM elements
    - Core principles: Separation of concerns, cascade conformance, performance optimization
    - System context: Provides computed styles to the LayoutSystem while consuming DOM from AngleSharp core
3. Ask if the user wants to modify or continue.
    

### Step 3: Component Architecture

1. Guide the user to create a Component Architecture section:
    
    ```
    ## 2. Component Architecture
    
    ### 2.1 Primary Components
    [List and describe the main components of the system]
    
    #### Component 1 Name
    - **Purpose**: [What this component does]
    - **Responsibilities**: [Key responsibilities]
    - **Interfaces**: [Key interfaces it exposes]
    - **Dependencies**: [What it depends on]
    ```
    
2. Provide suggestions for components based on the selected system. For example, for StyleSystem:
    
    - StyleEngine: Main orchestration component
    - StyleSheetManager: Manages stylesheets from different origins
    - SelectorMatcher: Matches selectors against elements
    - CascadeResolver: Resolves property conflicts
    - ValueComputer: Computes final property values
3. Ask if the user wants to add, modify, or continue.
    

### Step 4: Data Flow and Process Architecture

1. Guide the user to create a Data Flow section:
    
    ```
    ## 3. Data Flow and Process Architecture
    
    ### 3.1 Core Data Flows
    [Describe how data moves through the system]
    
    ### 3.2 Key Processes
    [Describe the main processes that occur in the system]
    ```
    
2. Provide suggestions for data flows based on the selected system. For example, for StyleSystem:
    
    - DOM elements → Selector matching → Cascade resolution → Inheritance processing → Value computation → Computed styles
3. Ask if the user wants to add, modify, or continue.
    

### Step 5: Integration with Other Systems

1. Guide the user to create an Integration section:
    
    ```
    ## 4. Integration with Other Systems
    
    ### 4.1 Integration with [System 1]
    - **Integration Points**: [Where/how systems connect]
    - **Data Exchange**: [What data is exchanged]
    - **Dependencies**: [Cross-system dependencies]
    ```
    
2. Provide suggestions for integrations based on the selected system. For example, for StyleSystem:
    
    - Integration with LayoutSystem: Provides computed styles for layout calculations
    - Integration with LifecycleSystem: Reacts to style invalidation events
    - Integration with CacheSystem: Uses caching for style computations
3. Ask if the user wants to add, modify, or continue.
    

### Step 6: Design Decisions and Rationale

1. Guide the user to create a Design Decisions section:
    
    ```
    ## 5. Design Decisions and Rationale
    
    ### 5.1 Key Design Decisions
    - **Decision 1**: [Description of the decision]
      - **Alternatives Considered**: [What else was considered]
      - **Rationale**: [Why this approach was chosen]
      - **Implications**: [What this means for the system]
    ```
    
2. Provide suggestions for design decisions based on the selected system. For example, for StyleSystem:
    
    - Decision: CSS variable resolution approach
    - Decision: Separation of calculation phases
3. Ask if the user wants to add, modify, or continue.
    

### Step 7: Performance Considerations

1. Guide the user to create a Performance Considerations section:
    
    ```
    ## 6. Performance Considerations
    
    ### 6.1 Performance Requirements
    [Describe any performance requirements or targets]
    
    ### 6.2 Optimization Strategies
    [Describe strategies used to optimize performance]
    ```
    
2. Provide suggestions for performance considerations based on the selected system.
    
3. Ask if the user wants to add, modify, or continue.
    

### Step 8: Extension Points and Flexibility

1. Guide the user to create an Extension Points section:
    
    ```
    ## 7. Extension Points and Flexibility
    
    ### 7.1 Extension Mechanisms
    [Describe how the system can be extended]
    
    ### 7.2 Customization Points
    [Describe how the system can be customized]
    ```
    
2. Provide suggestions for extension points based on the selected system.
    
3. Ask if the user wants to add, modify, or continue.
    

### Step 9: Error Handling and Resilience

1. Guide the user to create an Error Handling section:
    
    ```
    ## 8. Error Handling and Resilience
    
    ### 8.1 Error Handling Strategy
    [Describe the overall approach to error handling]
    
    ### 8.2 Resilience Mechanisms
    [Describe how the system handles failures and ensures reliability]
    ```
    
2. Provide suggestions for error handling strategies based on the selected system.
    
3. Ask if the user wants to add, modify, or continue.
    

### Step 10: Implementation Considerations

1. Guide the user to create an Implementation Considerations section:
    
    ```
    ## 9. Implementation Considerations
    
    ### 9.1 Key Implementation Challenges
    [Describe any particularly challenging aspects of implementation]
    
    ### 9.2 Dependencies and Requirements
    [List key dependencies and requirements]
    
    ### 9.3 Phased Implementation Strategy
    [Describe how the system can be implemented in phases]
    ```
    
2. Provide suggestions for implementation considerations based on the selected system.
    
3. Ask if the user wants to add, modify, or continue.
    

### Step 11: Appendices

1. Guide the user to create Appendices for interfaces and diagrams:
    
    ```
    ## Appendix A: Interface Definitions
    
    ### A.1 Public Interfaces
    [List and define key public interfaces]
    
    ## Appendix B: Architecture Diagrams
    
    ### B.1 Component Diagram
    [Insert or describe component diagram]
    ```
    
2. Provide suggestions for interfaces and diagrams based on the selected system.
    
3. Ask if the user wants to add, modify, or continue.
    

### Step 12: Document Finalization

1. Present the complete architecture document
2. Ask if the user wants to make any final changes
3. Suggest potential next steps:
    - Creating an Implementation Plan for this system
    - Creating an Implementation Status tracker
    - Creating architecture documents for other systems
    - Creating integration documentation

## Implementation Plan Creation Process

### Step 1: System Identification

1. Ask which system the implementation plan is for:
    
    - StyleSystem
    - LayoutSystem
    - LifecycleSystem
    - CacheSystem
    - Another custom system
2. Ask if there's an existing architecture document to reference.
    

### Step 2: Implementation Objectives

1. Guide the user to create an Implementation Objectives section:
    
    ```
    ## 1. Implementation Objectives
    
    - **Objective 1**: [Description]
    - **Objective 2**: [Description]
    - **Objective 3**: [Description]
    ```
    
2. Provide suggestions for objectives based on the selected system.
    
3. Ask if the user wants to modify or continue.
    

### Step 3: Phased Implementation Strategy

1. Guide the user to create a Phased Implementation Strategy:
    
    ```
    ## 2. Phased Implementation Strategy
    
    ### Phase 1: Foundation ([Timeline])
    
    #### Objectives
    - [Specific objectives for this phase]
    
    #### Key Components
    1. **Component 1**
       - Feature 1.1
       - Feature 1.2
    ```
    
2. Provide suggestions for phases and components based on the selected system.
    
3. Ask if the user wants to add more phases, modify, or continue.
    

### Step 4: Component Dependencies

1. Guide the user to create a Component Dependencies section:
    
    ```
    ## 3. Component Dependencies
    
    ### Critical Path
    [List dependencies in critical path]
    
    ### Dependency Matrix
    | Component | Depends On |
    |-----------|------------|
    | Component 1 | None |
    | Component 2 | Component 1 |
    ```
    
2. Provide suggestions for dependencies based on the components identified.
    
3. Ask if the user wants to modify or continue.
    

### Step 5: Implementation Approach

1. Guide the user to create an Implementation Approach section:
    
    ```
    ## 4. Implementation Approach
    
    ### Development Methodology
    [Approach to development]
    
    ### Testing Strategy
    - **Unit Testing**: [Approach]
    - **Integration Testing**: [Approach]
    ```
    
2. Provide suggestions for implementation approaches based on the selected system.
    
3. Ask if the user wants to modify or continue.
    

### Step 6: Risk Management

1. Guide the user to create a Risk Management section:
    
    ```
    ## 5. Risk Management
    
    ### Identified Risks
    | Risk | Probability | Impact | Mitigation Strategy |
    |------|------------|--------|---------------------|
    | [Risk 1] | High/Medium/Low | High/Medium/Low | [Mitigation approach] |
    ```
    
2. Provide suggestions for risks based on the selected system.
    
3. Ask if the user wants to modify or continue.
    

### Step 7: Implementation Milestones

1. Guide the user to create an Implementation Milestones section:
    
    ```
    ## 6. Implementation Milestones
    
    | Milestone | Target Date | Deliverables | Success Criteria |
    |-----------|-------------|--------------|------------------|
    | Phase 1 Complete | YYYY-MM-DD | [List of deliverables] | [Criteria] |
    ```
    
2. Provide suggestions for milestones based on the phases identified.
    
3. Ask if the user wants to modify or continue.
    

### Step 8: Document Finalization

1. Present the complete implementation plan
2. Ask if the user wants to make any final changes
3. Suggest potential next steps:
    - Creating an Implementation Status tracker
    - Creating a more detailed schedule
    - Setting up project management tools

## Implementation Status Creation Process

### Step 1: System Identification

1. Ask which system the implementation status is for:
    
    - StyleSystem
    - LayoutSystem
    - LifecycleSystem
    - CacheSystem
    - Another custom system
2. Ask if there's an existing architecture document and implementation plan to reference.
    

### Step 2: Implementation Status Summary

1. Guide the user to create an Implementation Status Summary:
    
    ```
    ## Implementation Status Summary
    
    | Component | Status | Priority | Assignee | Target Completion |
    |-----------|--------|----------|----------|-------------------|
    | Component 1 | ✅ Complete | High | [Name] | Completed |
    | Component 2 | 🔄 In Progress (80%) | High | [Name] | YYYY-MM-DD |
    ```
    
2. Provide suggestions for components based on the selected system or referenced documents.
    
3. Ask if the user wants to modify or continue.
    

### Step 3: Detailed Component Status

1. Guide the user to create Detailed Component Status sections:
    
    ```
    ## Detailed Component Status
    
    ### Component 1: [Name]
    
    **Status**: ✅ Complete
    
    **Implementation Details**:
    - ✅ Feature 1
    - ✅ Feature 2
    - ✅ Feature 3
    ```
    
2. For each component in the summary, create a detailed section.
    
3. Ask if the user wants to modify or continue.
    

### Step 4: Integration Status

1. Guide the user to create an Integration Status section:
    
    ```
    ## Integration Status
    
    ### Integration with [Other System 1]
    
    **Status**: 🔄 In Progress
    
    **Integration Points**:
    - ✅ Integration Point 1
    - 🔄 Integration Point 2
    ```
    
2. Provide suggestions for integrations based on the selected system.
    
3. Ask if the user wants to modify or continue.
    

### Step 5: Next Steps

1. Guide the user to create a Next Steps section:
    
    ```
    ## Next Steps
    
    ### Immediate Focus (Next 2 Weeks)
    1. [Task 1]
    2. [Task 2]
    
    ### Short-Term Roadmap (Next 1-2 Months)
    1. [Task 1]
    2. [Task 2]
    ```
    
2. Provide suggestions for next steps based on the component statuses.
    
3. Ask if the user wants to modify or continue.
    

### Step 6: Document Finalization

1. Present the complete implementation status
2. Ask if the user wants to make any final changes
3. Suggest how to maintain the status document:
    - Regular updates (weekly/bi-weekly)
    - Integration with project management tools
    - Communication with team members

## Final Notes

- Be adaptive to the user's needs and preferences
- Provide relevant examples from AngleSharp Layout Engine when possible
- Focus on clarity and completeness over volume
- Always suggest next steps to keep the documentation process moving forward