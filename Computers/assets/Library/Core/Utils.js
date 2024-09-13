
export function CommandResult(
    context,
    result = null,
    isSuccessful = true
) {
    const resultList = []
    if (result !== null && Array.isArray(result)) {
        resultList.push(...result)
    }

    if (result !== null && !Array.isArray(result)) {
        resultList.push(result)
    }

    return {
        ...context,
        lastCommandResult: resultList,
        lastCommandSuccessful: isSuccessful
    }
}

export function CommandResultWithContext(
    context,
    result = null,
    additionalContext = {},
    isSuccessful = true
) {
    return CommandResult(
        {
            ...context,
            ...additionalContext
        },
        result,
        isSuccessful
    )
}

export function CommandArguments(context, args) {
    const lastCommandResult = context.lastCommandResult ?? []
    return [
        ...args,
        ...lastCommandResult
    ]
}
