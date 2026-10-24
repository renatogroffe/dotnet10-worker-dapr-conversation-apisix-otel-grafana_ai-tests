local core = require("apisix.core")

local plugin_name = "bearer-to-apikey"

local _M = {
    version = 0.1,
    priority = 2600,
    name = plugin_name,
    schema = {
        type = "object",
        properties = {}
    }
}

function _M.check_schema(conf)
    return core.schema.check(_M.schema, conf)
end

function _M.rewrite(conf, ctx)
    local authorization =
        core.request.header(ctx, "Authorization")

    if not authorization then
        return 401, {
            message = "Missing Authorization header"
        }
    end

    local api_key =
        string.match(authorization, "^Bearer%s+([^%s]+)$")

    if not api_key then
        return 401, {
            message = "Invalid Bearer token format"
        }
    end

    -- Disponibiliza a chave para o key-auth.
    ngx.req.set_header("apikey", api_key)

    -- Nao encaminha o token original.
    ngx.req.clear_header("Authorization")
end

return _M