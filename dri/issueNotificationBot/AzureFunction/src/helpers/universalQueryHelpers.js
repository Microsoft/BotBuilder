function flattenNodesAndEdges(object) {
    if (typeof object != 'object' || object == null) {
        return object;
    }
    if (Array.isArray(object)) {
        object.forEach((item) => flattenNodesAndEdges(item));
    }

    if (object.nodes || object.node || object.edges) {
        object = object.nodes || object.node || object.edges;
    }

    Object.keys(object).forEach((key) => object[key] = flattenNodesAndEdges(object[key]));

    return object;
}

module.exports = { flattenNodesAndEdges };