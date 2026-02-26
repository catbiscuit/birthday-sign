# BirthDaySign

## 一、Fork 仓库

## 二、添加 Secret

**`Settings`-->`Secrets and variables`-->`Actions`-->`Secrets`-->`New repository secret`，添加以下secrets：**
- `CONF`：其值如下：
    
	```json
	{
		"Bark_Devicekey": "xxx",//Bark推送，不使用的话填空
		"Bark_Icon": "https://xxx/logo_2x.png",//Bark推送的icon
		"Smtp_Server": "smtp.qq.com",
		"Smtp_Port": 587,
		"Smtp_Email": "xxx@qq.com",//Email推送，发送者的邮箱，不使用的话填空
		"Smtp_Password": "xxxx",
		"Receive_Email_List": [//Email推送接收者列表，为空时不发送
			"xxx@qq.com"
		],
		"Distance": 2,//距离天数，提前多少天提醒。Distance=2，会提醒今天、明天、后天这3天
		"Peoples": [{//2个Date支持[".","-","/"]分隔。"0.2.23"或"2.23"，年份填0或不填年龄为未知
			"Name": "杨涛",
			"LunarDate": "1990.2.23",//农历生日，过农历生日才填。
			"SolarDate": "1990.3.19"//阳历生日，过阳历生日才填。
		}]
	}
    ```

## 三、运行

**`Actions`->`Run`->`Run workflow`**

## 四、查看运行结果

**`Actions`->`Run`->`build`**

