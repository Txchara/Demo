#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <math.h>
#include <windows.h>

#define MAX_VARS 100

typedef struct {
    char name[32];
    double value; // 存 ln(x)
} Var;

Var vars[MAX_VARS];
int var_count = 0;

// 查找变量
int find_var(const char* name) {
    for (int i = 0; i < var_count; i++) {
        if (strcmp(vars[i].name, name) == 0)
            return i;
    }
    return -1;
}

// 获取变量值
__declspec(dllexport) double get_var_value(const char* name) {
    int idx = find_var(name);
    if (idx >= 0) return vars[idx].value;
    return NAN; // 未定义返回 NAN
}

// 设置变量
__declspec(dllexport) void set_var_value(const char* name, double value) {
    int idx = find_var(name);
    if (idx >= 0) {
        vars[idx].value = value;
    } else {
        strcpy(vars[var_count].name, name);
        vars[var_count].value = value;
        var_count++;
    }
}

// 加法（log-sum-exp）
double e_add(double a, double b) { return log(exp(a) + exp(b)); }

// 乘法
double e_mul(double a, double b) { return a + b; }

// 减法
double e_sub(double a, double b) {
    double diff = exp(a) - exp(b);
    if (diff <= 0) return NAN;
    return log(diff);
}

// 除法
double e_div(double a, double b) { return a - b; }

// 执行运算
__declspec(dllexport) double execute_op(const char* target, const char* op, double val1, double val2) {
    double result;
    if (strcmp(op, "+") == 0) result = e_add(val1, val2);
    else if (strcmp(op, "*") == 0) result = e_mul(val1, val2);
    else if (strcmp(op, "-") == 0) result = e_sub(val1, val2);
    else if (strcmp(op, "/") == 0) result = e_div(val1, val2);
    else return NAN;

    set_var_value(target, result);
    return result;
}

// 清空变量表
__declspec(dllexport) void reset_vars() {
    var_count = 0;
}